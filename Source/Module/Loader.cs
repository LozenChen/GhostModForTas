using Celeste.Mod.GhostModForTas.Utils;

namespace Celeste.Mod.GhostModForTas.Module;

internal static class Loader {

    public static void Load() {
        AttributeUtils.Invoke<LoadAttribute>();
    }

    public static void Unload() {
        AttributeUtils.Invoke<UnloadAttribute>();
        HookHelper.Unload();
    }

    public static void Initialize() {
        HookHelper.InitializeAtFirst();
        AttributeUtils.Invoke<InitializeAttribute>();
        GhostModule.Instance.SaveSettings();
    }

    public static void LoadContent() {
        // do nothing
    }
}