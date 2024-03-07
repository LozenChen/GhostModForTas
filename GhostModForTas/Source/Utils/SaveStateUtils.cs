using Celeste.Mod.GhostModForTas.Entities;
using Celeste.Mod.GhostModForTas.Recorder;
using Celeste.Mod.SpeedrunTool.SaveLoad;
using System;
using System.Linq;

namespace Celeste.Mod.GhostModForTas.Utils;

internal static class SaveStateUtils {
    private static bool Installed => Everest.Modules.Any(module => module.Metadata?.Name == "SpeedrunTool");
    private static object action;

    private static Guid run;
    private static long ghostTime;
    private static long lastGhostTime;
    private static long currentTime;
    private static long lastCurrentTime;

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
        action = SaveLoadAction.SafeAdd((_, _) => {
            run = GhostRecorder.Run;
            ghostTime = GhostCompare.GhostTime;
            lastGhostTime = GhostCompare.LastGhostTime;
            currentTime = GhostCompare.CurrentTime;
            lastCurrentTime = GhostCompare.LastCurrentTime;
        }, (_, _) => {
            GhostRecorder.Run = run;
            GhostCompare.GhostTime = ghostTime;
            GhostCompare.LastGhostTime = lastGhostTime;
            GhostCompare.CurrentTime = currentTime;
            GhostCompare.LastCurrentTime = lastCurrentTime;
        });
    }

    private static void RemoveSaveLoadAction() {
        SaveLoadAction.Remove((SaveLoadAction)action);
    }
}