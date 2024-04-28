using Celeste.Mod.GhostModForTas.Recorder;
using Celeste.Mod.GhostModForTas.Replayer;
using Celeste.Mod.SpeedrunTool.SaveLoad;
using MonoMod.ModInterop;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.GhostModForTas.Utils;

internal static class SaveStateUtils {
    private static bool Installed => Everest.Modules.Any(module => module.Metadata?.Name == "SpeedrunTool");
    private static object action;

    private static long ghostTime;
    private static long lastGhostTime;
    private static long currentTime;
    private static long lastCurrentTime;
    private static DynamicData data;
    private static GhostRecorderEntity recorder;
    private static GhostReplayerEntity replayer;

    [Initialize]
    public static void Initialize() {
        if (Installed) {
            AddSaveLoadAction();
        }
    }

    [Unload]
    public static void Unload() {
        if (Installed) {
            RemoveSaveLoadAction();
        }
    }

    private static void AddSaveLoadAction() {
        StateManager.Instance.ClearState(); // game crash if you saved before, hot reload, and load state, (mostly crash for reason like some mod type does not exist in the tracker) so we need to clear state when game reload
        action = SaveLoadAction.SafeAdd((_, _) => {
            ghostTime = GhostCompare.GhostTime;
            lastGhostTime = GhostCompare.LastGhostTime;
            currentTime = GhostCompare.CurrentTime;
            lastCurrentTime = GhostCompare.LastCurrentTime;
            recorder = GhostRecorder.Recorder.DeepCloneShared();
            replayer = GhostReplayer.Replayer.DeepCloneShared();
            data = GhostRecorder.dyn_data.DeepCloneShared();
        }, (_, _) => {
            GhostCompare.GhostTime = ghostTime;
            GhostCompare.LastGhostTime = lastGhostTime;
            GhostCompare.CurrentTime = currentTime;
            GhostCompare.LastCurrentTime = lastCurrentTime;
            GhostRecorder.Recorder = recorder.DeepCloneShared();
            GhostReplayer.Replayer = replayer.DeepCloneShared();
            GhostRecorder.dyn_data = data.DeepCloneShared();
        }, () => {
            recorder = null;
            replayer = null;
            data = null;
        });
    }

    private static void RemoveSaveLoadAction() {
        SaveLoadAction.Remove((SaveLoadAction)action);
    }
}



internal static class TH_SaveStateUtils {
    private static long ghostTime;
    private static long lastGhostTime;
    private static long currentTime;
    private static long lastCurrentTime;
    private static DynamicData data;
    private static GhostRecorderEntity recorder;
    private static GhostReplayerEntity replayer;

    [Initialize]
    public static void Initialize() {
        typeof(orig_TASHelperImport).ModInterop();
        if (TASHelperImport.Installed) {
            AddSaveLoadAction();
        }
    }

    private static void AddSaveLoadAction() {
        TASHelperImport.AddSLAction((_, _) => {
            ghostTime = GhostCompare.GhostTime;
            lastGhostTime = GhostCompare.LastGhostTime;
            currentTime = GhostCompare.CurrentTime;
            lastCurrentTime = GhostCompare.LastCurrentTime;
            recorder = GhostRecorder.Recorder.TH_DeepCloneShared();
            replayer = GhostReplayer.Replayer.TH_DeepCloneShared();
            data = GhostRecorder.dyn_data.TH_DeepCloneShared();
        }, (_, _) => {
            GhostCompare.GhostTime = ghostTime;
            GhostCompare.LastGhostTime = lastGhostTime;
            GhostCompare.CurrentTime = currentTime;
            GhostCompare.LastCurrentTime = lastCurrentTime;
            GhostRecorder.Recorder = recorder.TH_DeepCloneShared();
            GhostReplayer.Replayer = replayer.TH_DeepCloneShared();
            GhostRecorder.dyn_data = data.TH_DeepCloneShared();
        }, () => {
            recorder = null;
            replayer = null;
            data = null;
        }, null, null);
    }
}

internal static class TASHelperImport {

    public static bool Installed => orig_TASHelperImport.AddSLAction is not null;
    public static void AddSLAction(Action<Dictionary<Type, Dictionary<string, object>>, Level> saveState, Action<Dictionary<Type, Dictionary<string, object>>, Level> loadState, Action clearState, Action<Level> beforeSaveState = null, Action preCloneEntities = null) {
        if (orig_TASHelperImport.AddSLAction is null) {
            return;
        }
        orig_TASHelperImport.AddSLAction(saveState, loadState, clearState, beforeSaveState, preCloneEntities);
    }

    public static T TH_DeepCloneShared<T>(this T obj) {
        if (orig_TASHelperImport.DeepCloneShared is null) {
            return default;
        }
        return (T)orig_TASHelperImport.DeepCloneShared(obj);
    }
}

[ModImportName("TASHelper")]
internal static class orig_TASHelperImport {
    public static Action<Action<Dictionary<Type, Dictionary<string, object>>, Level>, Action<Dictionary<Type, Dictionary<string, object>>, Level>, Action, Action<Level>, Action> AddSLAction;
    public static Func<object, object> DeepCloneShared;
}