using Celeste.Mod.GhostModForTas.Recorder;
using Celeste.Mod.GhostModForTas.Replayer;
using Celeste.Mod.SpeedrunTool.SaveLoad;
using System.Linq;

namespace Celeste.Mod.GhostModForTas.Utils;

internal static class SaveStateUtils {
    private static bool Installed => Everest.Modules.Any(module => module.Metadata?.Name == "SpeedrunTool");
    private static object action;

    private static long ghostTime;
    private static long lastGhostTime;
    private static long currentTime;
    private static long lastCurrentTime;
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
        }, (_, _) => {
            GhostCompare.GhostTime = ghostTime;
            GhostCompare.LastGhostTime = lastGhostTime;
            GhostCompare.CurrentTime = currentTime;
            GhostCompare.LastCurrentTime = lastCurrentTime;
            GhostRecorder.Recorder = recorder.DeepCloneShared();
            GhostReplayer.Replayer = replayer.DeepCloneShared();
        }, () => {
            recorder = null;
            replayer = null;
        });
    }

    private static void RemoveSaveLoadAction() {
        SaveLoadAction.Remove((SaveLoadAction)action);
    }
}