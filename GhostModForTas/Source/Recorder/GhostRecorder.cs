using Celeste.Mod.GhostModForTas.Module;
using Celeste.Mod.GhostModForTas.Recorder.Data;
using Celeste.Mod.GhostModForTas.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using TAS.Input.Commands;

namespace Celeste.Mod.GhostModForTas.Recorder;

internal static class GhostRecorder {

    public static string PathGhosts { get; internal set; }

    public static GhostRecorderEntity recorder;

    public static Guid Run;

    [Load]
    public static void Load() {
        PathGhosts = Path.Combine(Everest.PathSettings, "GhostsForTas");
        if (!Directory.Exists(PathGhosts)) {
            Directory.CreateDirectory(PathGhosts);
        }

        On.Celeste.Player.Die += OnDie;


        typeof(LevelExit).GetConstructor(new Type[] { typeof(LevelExit.Mode), typeof(Session), typeof(HiresSnow) }).IlHook(il => {
            ILCursor cursor = new(il);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate(OnExit);
        });
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Player.Die -= OnDie;
    }

    [LoadLevel]
    public static void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
        if (isFromLoader) { // from a load command / load into a new level normally / from a restart ...
            recorder?.RemoveSelf();
            recorder = null;
            Run = Guid.NewGuid();
        }

        if (playerInstance is null) {
            level.Add(new Entity { new Coroutine(WaitForPlayer(level)) });
        } else {
            Step(level);
        }
    }

    public static void StartRecording() {
        /*
        recorder?.RemoveSelf();
        recorder = null;
        Run = Guid.NewGuid();
        */
    }

    public static void GotoNextRoom() {

    }

    [TasDisableRun]
    public static void StopRecording() {

    }


    [TasCommand("GhostStartRecording", AliasNames = new[] { "GhostStartRecord", "StartGhostRecord", "StartGhostRecording" }, ExecuteTiming = ExecuteTiming.Runtime)]
    public static void StartRecordingCommand() {
        Logger.Log("Ghost", "[GhostStartRecording] Log Test");
    }

    [TasCommand("GhostStopRecording")]
    public static void StopRecordingCommand() {

    }

    private static IEnumerator WaitForPlayer(Level level) {
        while (level.Tracker.GetEntity<Player>() is null) {
            yield return null;
        }

        Step(level);
    }

    public static void OnExit(LevelExit.Mode mode) {
        if (Engine.Scene is not Level level) {
            return;
        }
        if (mode == LevelExit.Mode.Completed ||
            mode == LevelExit.Mode.CompletedInterlude) {
            Step(level);
        }
    }

    public static void Step(Level level) {
        if (ghostSettings.Mode == GhostModuleMode.Off) {
            return;
        }

        string target = level.Session.Level;
        Logger.Log("ghost", $"Stepping into {level.Session.Area.GetSID()} {target}");

        // Write the ghost, even if we haven't gotten an IL PB.
        // Maybe we left the level prematurely earlier?
        if (recorder?.Data != null &&
            (ghostSettings.Mode & GhostModuleMode.Record) == GhostModuleMode.Record) {
            recorder.Data.Target = target;
            recorder.Data.Run = Run;
            recorder.Data.Write();
        }

        if (recorder != null) {
            recorder.RemoveSelf();
        }
        level.Add(recorder = new GhostRecorderEntity());
        recorder.Data = new Data.GhostData(level.Session);
        recorder.Data.Name = ghostSettings.Name;
    }

    public static PlayerDeadBody OnDie(On.Celeste.Player.orig_Die orig, Player player, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats) {
        PlayerDeadBody corpse = orig(player, direction, evenIfInvincible, registerDeathInStats);

        if (recorder == null || recorder.Data == null) {
            return corpse;
        }

        // This is hacky, but it works:
        // Check the stack trace for Celeste.Level+* <Pause>*
        // and throw away the data when we're just retrying.
        foreach (StackFrame frame in new StackTrace().GetFrames()) {
            MethodBase method = frame?.GetMethod();
            if (method == null || method.DeclaringType == null) {
                continue;
            }

            if (!method.DeclaringType.FullName.StartsWith("Celeste.Level+") ||
                !method.Name.StartsWith("<Pause>")) {
                continue;
            }

            recorder.Data = null;
            return corpse;
        }

        return corpse;
    }
}
public class GhostRecorderEntity : Entity {
    public GhostData Data;

    public GhostFrame LastFrameData;

    public GhostRecorderEntity()
        : base() {
        Depth = 1000000;

        Tag = Tags.HUD;
    }

    public override void Update() {
        base.Update();

        RecordData();
    }

    public void RecordData() {
        if ((Engine.Scene as Level)?.Session is not Session session) {
            return;
        }

        if (playerInstance is not Player player) {
            LastFrameData = new GhostFrame { Data = new GhostChunkData { HasPlayer = false } };
            if (Data != null) {
                Data.Frames.Add(LastFrameData);
            }
            return;
        }

        // A data frame is always a new frame, no matter if the previous one lacks data or not.
        LastFrameData = new GhostFrame {
            Data = new GhostChunkData {
                HasPlayer = true,

                InControl = player.InControl,

                Position = player.Position,
                Speed = player.Speed,
                Rotation = player.Sprite.Rotation,
                Scale = player.Sprite.Scale,
                Color = player.Sprite.Color,

                Facing = player.Facing,

                CurrentAnimationID = player.Sprite.CurrentAnimationID,
                CurrentAnimationFrame = player.Sprite.CurrentAnimationFrame,

                HairColor = player.Hair.Color,
                HairSimulateMotion = player.Hair.SimulateMotion,

                DashColor = player.StateMachine.State == Player.StDash ? player.GetCurrentTrailColor() : (Color?)null,
                DashDir = player.DashDir,
                DashWasB = player.wasDashB,

                Time = session.Time
            }
        };

        if (player.StateMachine.State == Player.StRedDash) {
            LastFrameData.Data.HairCount = 1;
        } else if (player.StateMachine.State != Player.StStarFly) {
            LastFrameData.Data.HairCount = player.Dashes > 1 ? 5 : 4;
        } else {
            LastFrameData.Data.HairCount = 7;
        }

        if (Data != null) {
            Data.Frames.Add(LastFrameData);
        }
    }
}
