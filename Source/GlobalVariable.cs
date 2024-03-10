global using Celeste.Mod.GhostModForTas.Utils.Attributes;
global using static Celeste.Mod.GhostModForTas.GlobalVariables;
using Celeste.Mod.GhostModForTas.Module;
using Monocle;
using System;
using TAS.Module;

namespace Celeste.Mod.GhostModForTas;

internal static class GlobalVariables {

    public static string PathGhosts => Recorder.GhostRecorder.PathGhosts;

    public static CelesteTasSettings TasSettings => CelesteTasSettings.Instance;
    public static GhostModuleSettings ghostSettings => GhostModule.ModuleSettings;
    public static Player? playerInstance => Engine.Scene.Tracker.GetEntity<Player>();

    public static readonly object[] parameterless = { };
}


internal static class GlobalMethod {
    public static T Apply<T>(this T obj, Action<T> action) {
        action(obj);
        return obj;
    }

}