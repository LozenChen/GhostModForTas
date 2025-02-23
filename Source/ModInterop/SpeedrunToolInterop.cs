using Celeste.Mod.GhostModForTas.Recorder;
using Celeste.Mod.GhostModForTas.Replayer;
using Celeste.Mod.SpeedrunTool.SaveLoad;
using MonoMod.ModInterop;
using MonoMod.Utils;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.GhostModForTas.ModInterop;

internal static class SpeedrunToolInterop {

    public static bool SpeedrunToolInstalled;

    private static object action;

    [Initialize]
    public static void Initialize() {
        typeof(SpeedrunToolImport).ModInterop();
        SpeedrunToolInstalled = SpeedrunToolImport.RegisterSaveLoadAction is not null;
        AddSaveLoadAction();
    }

    [Unload]
    public static void Unload() {
        RemoveSaveLoadAction();
    }

    private static void AddSaveLoadAction() {
        if (!SpeedrunToolInstalled) {
            return;
        }

        action = SpeedrunToolImport.RegisterSaveLoadAction(
            (savedValues, _) => {
                savedValues[typeof(SpeedrunToolInterop)] = new Dictionary<string, object> {
                    { "ghostTime", GhostCompare.GhostTime },
                    { "lastGhostTime", GhostCompare.LastGhostTime },
                    { "currentTime", GhostCompare.CurrentTime },
                    { "lastCurrentTime", GhostCompare.LastCurrentTime},
                    { "recorder", GhostRecorder.Recorder},
                    { "replayer", GhostReplayer.Replayer},
                    { "data", GhostRecorder.dyn_data}
                }.DeepCloneShared();
            },
            (savedValues, _) => {
                Dictionary<string, object> clonedValues = savedValues.DeepCloneShared()[typeof(SpeedrunToolInterop)];
                GhostCompare.GhostTime = (long)clonedValues["ghostTime"];
                GhostCompare.LastGhostTime = (long)clonedValues["lastGhostTime"];
                GhostCompare.CurrentTime = (long)clonedValues["currentTime"];
                GhostCompare.LastCurrentTime = (long)clonedValues["lastCurrentTime"];
                GhostRecorder.Recorder = (GhostRecorderEntity)clonedValues["recorder"];
                GhostReplayer.Replayer = (GhostReplayerEntity)clonedValues["replayer"];
                GhostRecorder.dyn_data = (DynamicData)clonedValues["data"];
            },
            null, null, null, null
        );
    }

    private static void RemoveSaveLoadAction() {
        if (SpeedrunToolInstalled) {
            SpeedrunToolImport.Unregister(action);
        }
    }
}

[ModImportName("SpeedrunTool.SaveLoad")]
internal static class SpeedrunToolImport {

    public static Func<Action<Dictionary<Type, Dictionary<string, object>>, Level>, Action<Dictionary<Type, Dictionary<string, object>>, Level>, Action, Action<Level>, Action<Level>, Action, object> RegisterSaveLoadAction;

    public static Func<Type, string[], object> RegisterStaticTypes;

    public static Action<object> Unregister;
}