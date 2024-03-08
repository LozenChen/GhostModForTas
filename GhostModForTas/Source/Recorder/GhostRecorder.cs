using Celeste.Mod.GhostModForTas.Module;
using Celeste.Mod.GhostModForTas.Recorder.Data;
using Celeste.Mod.GhostModForTas.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using TAS.Input.Commands;

namespace Celeste.Mod.GhostModForTas.Recorder;

internal static class GhostRecorder {

    public static string PathGhosts { get; internal set; }

    public static GhostRecorderEntity Recorder;

    public static Guid Run;

    [Load]
    public static void Load() {
        PathGhosts = Path.Combine(Everest.PathSettings, "GhostsForTas");
        if (!Directory.Exists(PathGhosts)) {
            Directory.CreateDirectory(PathGhosts);
        }


        typeof(LevelExit).GetConstructor(new Type[] { typeof(LevelExit.Mode), typeof(Session), typeof(HiresSnow) }).IlHook(il => {
            ILCursor cursor = new(il);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate(OnExit);
        });

        On.Celeste.Session.ctor += OnSessionCtor;
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Session.ctor -= OnSessionCtor;
    }

    [LoadLevel]
    public static void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
        if (isFromLoader) { // from a load command / load into a new level normally / from a restart ...
            Recorder?.RemoveSelf();
            level.Add(Recorder = new GhostRecorderEntity(level.Session));
        }

        Step(level);
    }

    private static void OnSessionCtor(On.Celeste.Session.orig_ctor orig, Session self) {
        Run = Guid.NewGuid();
        orig(self);
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

    public static void OnExit(LevelExit.Mode mode) {
        if (Engine.Scene is not Level level) {
            return;
        }
        if (mode == LevelExit.Mode.Completed ||
            mode == LevelExit.Mode.CompletedInterlude) {
            Step(level, levelExit: true);
        }
    }

    public static void Step(Level level, bool levelExit = false) {
        if (Recorder?.Data != null &&
            (ghostSettings.Mode & GhostModuleMode.Record) == GhostModuleMode.Record) {
            string target = levelExit ? "LevelExit" : level.Session.Level;
            if (levelExit) {
                Recorder.Data.Target = target;
                Recorder.Data.Run = Run;
                Recorder.WriteData();
            } else if (target != Recorder.Data.Level) {
                Recorder.Data.Target = target;
                Recorder.Data.Run = Run;
                Recorder.WriteData();
                Recorder.Data = new Data.GhostData(level.Session);
                Recorder.Data.Name = ghostSettings.Name;
            }
            // otherwise it's a respawn or something
        }
    }
}
public class GhostRecorderEntity : Entity {
    public GhostData Data;

    public GhostFrame LastFrameData;

    public Dictionary<string, int> RevisitCount;

    public GhostRecorderEntity(Session session)
        : base() {
        Depth = 1000000;
        Tag = Tags.HUD | Tags.FrozenUpdate | Tags.PauseUpdate | Tags.TransitionUpdate | Tags.Persistent;
        RevisitCount = new();
        Data = new GhostData(session);
    }

    public void WriteData() {
        if (RevisitCount.ContainsKey(Data.Level)) {
            RevisitCount[Data.Level]++;
        } else {
            RevisitCount[Data.Level] = 1;
        }
        Data.LevelVisitCount = RevisitCount[Data.Level];
        Data.TargetVisitCount = RevisitCount.TryGetValue(Data.Target, out int targetCount) ? targetCount + 1 : 1;
        Data.Write();
    }

    public override void Update() {
        base.Update();

        RecordData();
    }

    public void RecordData() {
        if (Engine.Scene is not Level level || level.Session is not Session session) {
            return;
        }

        if (playerInstance is not Player player) {
            LastFrameData = new GhostFrame { ChunkData = new GhostChunkData { HasPlayer = false } };
            Data.Frames.Add(LastFrameData);
            return;
        }

        // A data frame is always a new frame, no matter if the previous one lacks data or not.
        LastFrameData = new GhostFrame {
            ChunkData = new GhostChunkData {
                HasPlayer = true,
                UpdateHair = level.updateHair,
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
            LastFrameData.ChunkData.HairCount = 1;
        } else if (player.StateMachine.State != Player.StStarFly) {
            LastFrameData.ChunkData.HairCount = player.Dashes > 1 ? 5 : 4;
        } else {
            LastFrameData.ChunkData.HairCount = 7;
        }

        Data.Frames.Add(LastFrameData);
    }
}
