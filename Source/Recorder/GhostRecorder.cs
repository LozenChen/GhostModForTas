using Celeste.Mod.GhostModForTas.Module;
using Celeste.Mod.GhostModForTas.Recorder.Data;
using Celeste.Mod.GhostModForTas.Replayer;
using Celeste.Mod.GhostModForTas.Utils;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TAS;
using TAS.EverestInterop.InfoHUD;
using TAS.Input.Commands;
using TAS.Module;

namespace Celeste.Mod.GhostModForTas.Recorder;

internal static class GhostRecorder {

    public static string PathGhosts { get; internal set; }

    public static GhostRecorderEntity Recorder;

    public static Guid Run;

    internal static bool IsFreezeFrame = false;

    public static long RTASessionTime = 0L;

    private static bool isSoftlockReloading = false;

    public static GhostModuleMode? origMode;

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

    private static void OnSessionCtor(On.Celeste.Session.orig_ctor orig, Session self) {
        Run = Guid.NewGuid();
        RTASessionTime = 0L;
        orig(self);
    }

    [LoadLevel]
    public static void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
        // softlock
        if (isSoftlockReloading) {
            level.Add(Recorder);
            level.Add(GhostReplayer.Replayer);
            GhostReplayer.Replayer?.Ghosts?.ForEach(level.Add);
            isSoftlockReloading = false;
            return;
        }

        // recorder
        if (LoadLevelDetector.IsStartingLevel(level, isFromLoader)) {
            Recorder?.RemoveSelf();
            CachedEntitiesForParse.Clear();
            level.Add(Recorder = new GhostRecorderEntity(level.Session));
        }
        Step(level);
        if (level.Session.Time == 0L) { // a restart
            RTASessionTime = 0L;
        }

        // replayer
        if (!ghostSettings.Mode.Has(GhostModuleMode.Play)) {
            GhostReplayer.Replayer?.RemoveSelf();
        } else {
            if (LoadLevelDetector.IsStartingLevel(level, isFromLoader)) {
                GhostReplayer.Replayer?.RemoveSelf();
                level.Add(GhostReplayer.Replayer = new GhostReplayerEntity(level));
            } else if (!isFromLoader && level.Tracker.GetEntity<GhostReplayerEntity>() is { } replayer) {
                GhostReplayer.Replayer = replayer;
                GhostReplayer.Replayer.HandleTransition(level);
            }
        }

    }

    [FreezeUpdate]

    public static void UpdateInFreezeFrame() {
        IsFreezeFrame = true;
        Recorder?.Update();
        if (Engine.Scene is Level level) {
            GhostRecorderEntity.RestoreHudInfo(level, true);
        }
        IsFreezeFrame = false;
    }

    [Initialize]
    private static void Initialize() {
        typeof(Engine).GetMethodInfo("Update").IlHook(il => {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(ins => ins.MatchLdsfld<Engine>(nameof(Engine.DashAssistFreeze)))) {
                cursor.MoveAfterLabels();
                cursor.EmitDelegate(IncreaseRTATimer);
            }
        });

        typeof(Level).GetNestedType("<TransitionRoutine>d__29", System.Reflection.BindingFlags.NonPublic).GetMethodInfo("MoveNext").IlHook(il => {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(ins => ins.MatchCall<TimeSpan>("get_TotalSeconds"), ins => ins.MatchLdcR8(5))) {
                cursor.Index += 10;
                cursor.EmitDelegate(EscapeFromAntiSoftlock);
            }
        });
    }

    private static void IncreaseRTATimer() {
        RTASessionTime += 170000L;
    }


    private static void EscapeFromAntiSoftlock() {
        isSoftlockReloading = true;
        if (Recorder is not null) {
            Recorder.Scene = null;
        }
        if (GhostReplayer.Replayer is not null) {
            GhostReplayer.Replayer.Scene = null;
            GhostReplayer.Replayer.Ghosts.ForEach(x => x.Scene = null);
        }
    }

    [TasCommand("StopGhostRecording", AliasNames = new[] { "GhostStopRecord", "StopGhostRecord", "GhostStopRecording" }, ExecuteTiming = ExecuteTiming.Runtime)]
    public static void StopRecordingCommand() {
        if (origMode.HasValue) {
            ghostSettings.Mode = origMode.Value & GhostModuleMode.Play; // recording is stopped anyway
            origMode = null;
        }
    }

    [TasDisableRun]
    private static void OnTasDisableRun() {
        StopRecordingCommand();
    }


    [TasCommand("StartGhostRecording", AliasNames = new[] { "GhostStartRecord", "StartGhostRecord", "GhostStartRecording", "GhostRecord", "GhostRecording", "RecordGhost", "RecordingGhost" }, ExecuteTiming = ExecuteTiming.Runtime)]
    public static void StartRecordingCommand() {
        origMode = ghostSettings.Mode;
        ghostSettings.Mode = GhostModuleMode.Record;
    }

    [TasCommand("GhostReplay", AliasNames = new[] { "GhostPlay", "ReplayGhost", "PlayGhost", "GhostPlayMode", "PlayGhostMode", "GhostReplayMode", "ReplayGhostMode" }, ExecuteTiming = ExecuteTiming.Runtime)]
    public static void GhostReplayCommand() {
        ghostSettings.Mode = GhostModuleMode.Play;
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
                Recorder.Data.TargetCount = new(target, 1);
                Recorder.Data.Run = Run;
                Recorder.Data.IsCompleted = true;
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

    public static Dictionary<string, List<Entity>> CachedEntitiesForParse = new();
    public static string ParseTemplate() {
        return TAS.EverestInterop.InfoHUD.InfoCustom.ParseTemplate(ghostSettings.CustomInfoTemplate, CelesteTasSettings.Instance.CustomInfoDecimals, CachedEntitiesForParse, false);
    }
}
public class GhostRecorderEntity : Entity {
    public GhostData Data;

    public GhostFrame LastFrameData;

    public Dictionary<string, int> RevisitCount;

    public static string HudInfo;

    public GhostRecorderEntity(Session session)
        : base() {
        Depth = -10000000;
        Tag = Tags.HUD | Tags.FrozenUpdate | Tags.PauseUpdate | Tags.TransitionUpdate | Tags.Global;
        RevisitCount = new();
        Data = new GhostData(session);
        lastFrameHudInfo = "";
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
        Data.RTASessionTime = Data.Frames.LastOrDefault().ChunkData.RTATime;
        Data.Write();
    }

    public override void Update() {
        base.Update();
        RecordData();
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
        GhostRecorder.Recorder = null;
    }

    public void RecordData() {
        if (Engine.Scene is not Level level || level.Session is not Session session) {
            return;
        }
        if ((GhostModule.ModuleSettings.Mode & GhostModuleMode.Record) != GhostModuleMode.Record) {
            RemoveSelf();
            return;
        }

        if (playerInstance is not Player player) {
            LastFrameData = new GhostFrame { ChunkData = new GhostChunkData { HasPlayer = false, Time = session.Time, RTATime = GhostRecorder.RTASessionTime } };
            Data.Frames.Add(LastFrameData);
            return;
        }

        // A data frame is always a new frame, no matter if the previous one lacks data or not.
        LastFrameData = new GhostFrame {
            ChunkData = new GhostChunkData {

                Time = session.Time,
                RTATime = GhostRecorder.RTASessionTime,
                IsFreezeFrame = GhostRecorder.IsFreezeFrame,
                HasPlayer = true,

                Position = player.Position,
                Subpixel = player.movementCounter, // this is unncessary for rendering, but we need it in info hud to draw subpixel indicator
                Speed = player.Speed,
                HitboxWidth = player.Collider.Width,
                HitboxHeight = player.Collider.Height,
                HitboxLeft = player.Collider.Position.X,
                HitboxTop = player.Collider.Position.Y,
                HurtboxWidth = player.hurtbox.width,
                HurtboxHeight = player.hurtbox.height,
                HurtboxLeft = player.hurtbox.Position.X,
                HurtboxTop = player.hurtbox.Position.Y,
                //HudInfo = ghostSettings.ShowHudInfo ? lastFrameHudInfo : "",
                // by OoO, Entities Update -> LoadLevel (which happens in Scene.OnEndOfFrame, in Scene.AfterUpdate) -> TAS Update hud info
                // so we can't get an accurate readtime hud info here
                //CustomInfo = ghostSettings.ShowCustomInfo ? GhostRecorder.ParseTemplate() : "",

                HudInfo = lastFrameHudInfo,
                CustomInfo = GhostRecorder.ParseTemplate(),

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

    private static string lastFrameHudInfo = "";
    public static string GetHudInfo() {
        if (!TasSettings.Enabled || !TasSettings.InfoHud) {
            return "";
        }
        StringBuilder stringBuilder = new();

        if (TasSettings.InfoTasInput) {
            InfoHud.WriteTasInput(stringBuilder);
        }

        string hudInfo = GameInfo.HudInfo;
        if (hudInfo.IsNotEmpty()) {
            if (stringBuilder.Length > 0) {
                stringBuilder.AppendLine();
            }

            stringBuilder.Append(hudInfo);
        }

        return stringBuilder.ToString().Trim();
    }


    private static void SceneOnAfterUpdate(On.Monocle.Scene.orig_AfterUpdate orig, Scene self) {
        orig(self);
        if (self is Level level) {
            RestoreHudInfo(level);
        }
    }

    internal static void RestoreHudInfo(Level level, bool isFreezeFrame = false) {
        if ((GhostModule.ModuleSettings.Mode & GhostModuleMode.Record) != GhostModuleMode.Record) {
            return;
        }
        if (isFreezeFrame) {
            GameInfo.Update();
        } else if (Manager.UltraFastForwarding) {
            GameInfo.Update(!level.wasPaused);
        }
        lastFrameHudInfo = GetHudInfo();
    }

    [Load]
    private static void Load() {
        using (DetourContext context = new DetourContext() { After = new List<string>() { "CelesteTAS-EverestInterop" } }) {
            On.Monocle.Scene.AfterUpdate += SceneOnAfterUpdate;
        }
    }

    [Unload]
    private static void Unload() {
        On.Monocle.Scene.AfterUpdate -= SceneOnAfterUpdate;
    }
}
