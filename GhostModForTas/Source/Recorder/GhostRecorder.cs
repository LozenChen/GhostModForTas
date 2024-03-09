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
using System.Linq;
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
        if (LoadLevelDetector.IsStartingLevel(level, isFromLoader)) {
            Recorder?.RemoveSelf();
            level.Add(Recorder = new GhostRecorderEntity(level.Session));
        }

        Step(level);
    }

    [FreezeUpdate]

    public static void UpdateInFreezeFrame() {
        IsFreezeFrame = true;
        Recorder?.Update();
        IsFreezeFrame = false;
    }

    internal static bool IsFreezeFrame = false;

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
            string target = levelExit ? LevelCount.Exit.Level : level.Session.Level;
            if (levelExit) {
                Recorder.Data.TargetCount = new (target, 1);
                Recorder.Data.Run = Run;
                Recorder.WriteData();
            } else if (target != Recorder.Data.LevelCount.Level) {
                Recorder.Data.TargetCount.Level = target;
                Recorder.Data.Run = Run;
                Recorder.WriteData();
                Recorder.Data = new Data.GhostData(level.Session);
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
        Depth = -10000000;
        Tag = Tags.HUD | Tags.FrozenUpdate | Tags.PauseUpdate | Tags.TransitionUpdate | Tags.Global;
        RevisitCount = new();
        Data = new GhostData(session);
    }

    public void WriteData() {
        if (RevisitCount.ContainsKey(Data.LevelCount.Level)) {
            RevisitCount[Data.LevelCount.Level]++;
        } else {
            RevisitCount[Data.LevelCount.Level] = 1;
        }
        Data.LevelCount.Count = RevisitCount[Data.LevelCount.Level];
        Data.TargetCount.Count = RevisitCount.TryGetValue(Data.TargetCount.Level, out int targetCount) ? targetCount + 1 : 1;
        Data.SessionTime = Data.Frames.LastOrDefault().ChunkData.Time;
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

                Time = session.Time,
                HasPlayer = true,


                Position = player.Position,
                Speed = player.Speed,

                UpdateHair = !GhostRecorder.IsFreezeFrame && level.updateHair,
                Rotation = player.Sprite.Rotation,
                Scale = player.Sprite.Scale,
                Color = player.Sprite.Color,
                Facing = player.Facing,
                CurrentAnimationID = player.Sprite.CurrentAnimationID,
                CurrentAnimationFrame = player.Sprite.CurrentAnimationFrame,

                HairColor = player.Hair.Color,
                HairSimulateMotion = player.Hair.SimulateMotion,
                HairCount = player.Sprite.HairCount
            }
        };

        Data.Frames.Add(LastFrameData);
    }
}
