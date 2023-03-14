using System;
using System.Linq;
using Celeste.Mod.SpeedrunTool.SaveLoad;

namespace Celeste.Mod.Ghost;

internal static class SaveStateUtils {
    private static bool Installed => Everest.Modules.Any(module => module.Metadata?.Name == "SpeedrunTool");
    private static object action;

    private static Guid run;
    private static long ghostTime;
    private static long lastGhostTime;
    private static long currentTime;
    private static long lastCurrentTime;

    public static void Initialize() {
        if (Installed) {
            AddSaveLoadAction();
        }
    }

    public static void Unload() {
        if (Installed) {
            RemoveSaveLoadAction();
        }
    }

    private static void AddSaveLoadAction() {
        action = SaveLoadAction.SafeAdd((_, _) => {
            run = GhostModule.Instance.Run;
            ghostTime = GhostCompareTime.GhostTime;
            lastGhostTime = GhostCompareTime.LastGhostTime;
            currentTime = GhostCompareTime.CurrentTime;
            lastCurrentTime = GhostCompareTime.LastCurrentTime;
        }, (_, _) => {
            GhostModule.Instance.Run = run;
            GhostCompareTime.GhostTime = ghostTime;
            GhostCompareTime.LastGhostTime = lastGhostTime;
            GhostCompareTime.CurrentTime = currentTime;
            GhostCompareTime.LastCurrentTime = lastCurrentTime;
        });
    }

    private static void RemoveSaveLoadAction() {
        SaveLoadAction.Remove((SaveLoadAction)action);
    }
}