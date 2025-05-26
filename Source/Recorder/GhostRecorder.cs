using Celeste.Mod.GhostModForTas.ModInterop;
using Celeste.Mod.GhostModForTas.Module;
using Celeste.Mod.GhostModForTas.Recorder.Data;
using Celeste.Mod.GhostModForTas.Replayer;
using Celeste.Mod.GhostModForTas.Utils;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using StudioCommunication.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TAS;
using TAS.EverestInterop.InfoHUD;
using TAS.Module;

namespace Celeste.Mod.GhostModForTas.Recorder;

internal static class GhostRecorder {

    public static string PathGhosts { get; internal set; }

    public static GhostRecorderEntity Recorder;

    public static Guid Run => (dyn_data?.TryGet(guidName, out object val) ?? false) ? (Guid)val : Guid.Empty;

    internal static bool IsFreezeFrame = false;

    internal static DynamicData dyn_data;

    public static long RTASessionTime {
        get => (dyn_data?.TryGet(rtaCounterName, out long val) ?? false) ? val : 0L;
        set {
            if (Engine.Scene is Level) {
                dyn_data?.Set(rtaCounterName, value);
            }
        }
    }

    private static bool isSoftlockReloading = false;

    public static GhostModuleMode? origMode;

    public const string guidName = "GhostModForTas:GUID";

    public const string rtaCounterName = "GhostModForTas:RTASessionTime";

    [Load]
    public static void Load() {
        PathGhosts = Path.Combine(Everest.PathSettings, "GhostsForTas");
        if (!Directory.Exists(PathGhosts)) {
            Directory.CreateDirectory(PathGhosts);
        }

        Everest.Events.Level.OnExit += LevelOnExit;
        Everest.Events.Level.OnComplete += LevelOnComplete;
        On.Celeste.Session.ctor += OnSessionCtor;
    }

    [Unload]
    public static void Unload() {
        Everest.Events.Level.OnExit -= LevelOnExit;
        Everest.Events.Level.OnComplete -= LevelOnComplete;
        On.Celeste.Session.ctor -= OnSessionCtor;
    }
    private static void LevelOnExit(Level level, LevelExit _, LevelExit.Mode mode, Session __, HiresSnow ___) {
        if (mode == LevelExit.Mode.Completed ||
            mode == LevelExit.Mode.CompletedInterlude) {
            level.OnEndOfFrame += () => OnLevelEnd(level);
            // level exit or level complete are called when some entity update
            // and we can not guarantee that our Recorder updates after it
            // so we do it when EndOfFrame (which is still before (level = Engine.Scene) becomes nextScene)
        }
    }

    private static void LevelOnComplete(Level level) {
        level.OnEndOfFrame += () => OnLevelEnd(level);
    }

    [Monocle.Command("ghost_mark_level_end", "Manually instruct GhostModForTas that current level is ended. E.g. use it when you return to map after collecting cassettes.")]
    public static void ManuallyMarkLevelEnd() {
        if (Engine.Scene is Level level) {
            level.OnEndOfFrame += () => OnLevelEnd(level);
        }
    }

    private static void OnLevelEnd(Level level) {
        Step(level, levelExit: true);
        GhostReplayer.Replayer?.OnLevelEnd(level);
    }

    private static void OnSessionCtor(On.Celeste.Session.orig_ctor orig, Session self) {
        orig(self);
        DynamicData.For(self).Set(guidName, Guid.NewGuid());
        DynamicData.For(self).Set(rtaCounterName, 0L);
        // we don't use class static field here
        // coz session may get created during our play
        // e.g. when you use a new saveslot, and go to a checkpoint, then Checkpoint.Update -> Level.AutoSave -> ... -> UserIO.SaveThread -> UserIO.Save<(Mod)SaveData> will finally creates 3 sessions
    }


    [LoadLevel]
    public static void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
        // softlock
        dyn_data = DynamicData.For(level.Session);

        if (isSoftlockReloading) {
            Recorder?.Apply(level.Add);
            GhostReplayer.Replayer?.Apply(level.Add);
            GhostReplayer.Replayer?.Ghosts?.ForEach(level.Add);
            isSoftlockReloading = false;
            return;
        }

        // recorder
        if (!ghostSettings.Mode.Has(GhostModuleMode.Record)) {
            Recorder?.RemoveSelf();
        } else if (LoadLevelDetector.IsStartingLevel(level, isFromLoader)) {
            CreateNewRecorder(level);
        }
        Step(level);
        if (level.Session.Time == 0L) { // a restart
            RTASessionTime = 0L;
        }

        // replayer
        if (ghostSettings.Mode.Has(GhostModuleMode.Play)) {
            if (LoadLevelDetector.IsStartingLevel(level, isFromLoader)) {
                CreateNewReplayer(level);
            } else if (!isFromLoader && level.Tracker.GetEntity<GhostReplayerEntity>() is { } replayer) {
                GhostReplayer.Replayer = replayer;
                GhostReplayer.Replayer.HandleTransition(level);
            }
        }
    }

    internal static void CreateNewRecorder(Level level) {
        Recorder?.RemoveSelf();
        CachedEntitiesForParse.Clear();
        level.Add(Recorder = new GhostRecorderEntity(level.Session));
    }

    internal static void CreateNewRecorderOnEndOfFrame() {
        if (ghostSettings.Mode.HasFlag(GhostModuleMode.Record) && Engine.Scene is Level level && (GhostRecorder.Recorder is null || GhostRecorder.Recorder.Scene != level)) {
            Recorder?.RemoveSelf();
            CachedEntitiesForParse.Clear();
            level.Add(Recorder = new GhostRecorderEntity(level.Session));
        }
    }

    private static void CreateNewReplayer(Level level) {
        GhostReplayer.Replayer?.RemoveSelf();
        level.Add(new GhostReplayerEntity(level)); // the ctor already sets GhostReplayer.Replayer = this
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

    [UnpauseUpdate]
    [SkippingCutsceneUpdate]
    public static void UpdateInSpecialFrame() {
        Recorder?.Update();
        if (Engine.Scene is Level level) {
            GhostRecorderEntity.RestoreHudInfo(level, true);
        }
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

        if (typeof(Level).GetNestedType(TransitionRoutine_CompilerGeneratedName, BindingFlags.NonPublic) is { } transitionRoutine) {
            transitionRoutine.GetMethodInfo("MoveNext").IlHook(il => {
                ILCursor cursor = new ILCursor(il);
                if (cursor.TryGotoNext(ins => ins.MatchCall<TimeSpan>("get_TotalSeconds"), ins => ins.MatchLdcR8(5))) {
                    cursor.Index += 10;
                    cursor.EmitDelegate(EscapeFromAntiSoftlock);
                }
            });
        } else {
            Type routine = typeof(Level).GetNestedTypes(BindingFlags.NonPublic).
                First(x => x.Name.StartsWith("<TransitionRoutine>d__") &&
                            x.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(y => y.Name.StartsWith("<lightingStart>")).IsEmpty()
                );
            if (routine is not null) {
                Logger.Log(LogLevel.Error, "GhostModForTas", $"Can't find Level.{TransitionRoutine_CompilerGeneratedName}, use fallback {routine.Name} instead");
                // https://discord.com/channels/403698615446536203/1257712832972193792/1299678971851702374

                routine.GetMethodInfo("MoveNext").IlHook(il => {
                    ILCursor cursor = new ILCursor(il);
                    if (cursor.TryGotoNext(ins => ins.MatchCall<TimeSpan>("get_TotalSeconds"), ins => ins.MatchLdcR8(5))) {
                        cursor.Index += 10;
                        cursor.EmitDelegate(EscapeFromAntiSoftlock);
                    }
                });
            } else {
                throw new Exception($"[GhostModForTas] {nameof(GhostRecorder)}.{nameof(Initialize)}(): Can't find Level.{TransitionRoutine_CompilerGeneratedName}, and can't find a fallback!");
            }
        }
    }

    private const string TransitionRoutine_CompilerGeneratedName = "<TransitionRoutine>d__35"; // need Everest 1.5326, may change if everest patch level again

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


    [DisableRun]
    private static void OnTasDisableRun() {
        if (origMode.HasValue) {
            ghostSettings.Mode = origMode.Value & GhostModuleMode.Play; // recording is stopped anyway, but if originally replaying, then ok
            origMode = null;
            ghostSettings.UpdateStateText();
        }

        // recorder will remove itself when it finds RECORD mode is gone, we dont want recording to continue in this case
        // replayer will just hide itself instead, coz we may want to show the ghosts later
    }


    public static void StopRecordingCommand() {
        if (ghostSettings.Mode.HasFlag(GhostModuleMode.Record) && origMode.HasValue) {
            ghostSettings.Mode = origMode.Value & GhostModuleMode.Play; // recording is stopped anyway
            origMode = null;
            ghostSettings.UpdateStateText();
        }
        // the recorder will remove itself when it finds that RECORD mode is gone
    }

    public static void StartRecordingCommand() {
        origMode ??= ghostSettings.Mode;
        if (Engine.Scene is Level level && (Recorder is null || Recorder.Scene != level)) {
            CreateNewRecorder(level);
        }
        ghostSettings.Mode = GhostModuleMode.Record;
        ghostSettings.UpdateStateText();
    }

    public static void StartGhostReplayCommand() {
        origMode ??= ghostSettings.Mode;
        if (Engine.Scene is Level level && (GhostReplayer.Replayer is null || GhostReplayer.Replayer.Scene != level)) {
            CreateNewReplayer(level);
        }
        ghostSettings.Mode = GhostModuleMode.Play;
        ghostSettings.UpdateStateText();
    }

    public static void GhostReplayReloadCommand() {
        origMode ??= ghostSettings.Mode;
        if (Engine.Scene is Level level) {
            CreateNewReplayer(level); // forces the game to re-create a replayer. so if there are new ghosts, the replayer will find them
        }
        ghostSettings.Mode = GhostModuleMode.Play;
        ghostSettings.UpdateStateText();
    }

    public static void StopReplayCommand() {
        if (ghostSettings.Mode.HasFlag(GhostModuleMode.Play) && origMode.HasValue) {
            ghostSettings.Mode = origMode.Value & GhostModuleMode.Record; // replaying is stopped anyway
            origMode = null;
            ghostSettings.UpdateStateText();
        }
    }

    public static void Step(Level level, bool levelExit = false) {
        if (Recorder?.Data != null &&
            (ghostSettings.Mode.HasFlag(GhostModuleMode.Record))) {
            string target = levelExit ? LevelCount.Exit.Level : level.Session.Level;
            if (levelExit) {
                Recorder.Data.TargetCount = new(target, 1);
                Recorder.Data.Run = Run;
                Recorder.Data.IsCompleted = true;
                Recorder.WriteData();
                Recorder.Data = null;
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
        return string.Join('\n', TAS.InfoHUD.InfoCustom.ParseTemplate(StringExtensions.SplitLines(ghostSettings.CustomInfoTemplate), CelesteTasSettings.Instance.CustomInfoDecimals));
    }
}

[Tracked(false)]
public class GhostRecorderEntity : Entity {
    public GhostData Data;

    public GhostFrame LastFrameData;

    public Dictionary<string, int> RevisitCount;

    public static string HudInfo;

    public static bool updateHair = true; // some OoO issue

    public GhostRecorderEntity(Session session)
        : base() {
        Depth = -10000000;
        Tag = Tags.HUD | Tags.FrozenUpdate | Tags.PauseUpdate | Tags.TransitionUpdate | Tags.Global;
        RevisitCount = new();
        Data = new GhostData(session);
        lastFrameHudInfo = "";
        Logger.Log("GhostModForTas", "Recorder added");
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
        Logger.Log("GhostModForTas", "Recorder removed");
    }

    public void RecordData() {
        if (Engine.Scene is not Level level || level.Session is not Session session || Data is null) {
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
                // HudInfo = ghostSettings.ShowHudInfo ? lastFrameHudInfo : "",
                // by OoO, Entities Update -> LoadLevel (which happens in Scene.OnEndOfFrame, in Scene.AfterUpdate) -> TAS Update hud info
                // so we can't get an accurate readtime hud info here
                // CustomInfo = ghostSettings.ShowCustomInfo ? GhostRecorder.ParseTemplate() : "",

                HudInfo = lastFrameHudInfo,
                CustomInfo = GhostRecorder.ParseTemplate(),

                UpdateHair = !GhostRecorder.IsFreezeFrame && updateHair,
                Rotation = player.Sprite.Rotation,
                Scale = player.Sprite.Scale,
                Color = player.Sprite.Color,
                Facing = player.Facing,
                CurrentAnimationID = player.Sprite.CurrentAnimationID,
                CurrentAnimationFrame = player.Sprite.CurrentAnimationFrame,

                HairColor = player.Hair.Color,
                HairSimulateMotion = player.Hair.SimulateMotion,
                HairCount = player.Sprite.HairCount,
                IsInverted = ModImports.IsPlayerInverted,
            }
        };
        updateHair = level.updateHair;

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
        } else if (Manager.FastForwarding) {
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
