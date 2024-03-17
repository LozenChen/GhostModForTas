using Celeste.Mod.GhostModForTas.Utils;

namespace Celeste.Mod.GhostModForTas.Module;

internal static class Loader {

    public static void Load() {
        Reloading = GFX.Loaded;
        AttributeUtils.Invoke<LoadAttribute>();
    }

    public static void Unload() {
        AttributeUtils.Invoke<UnloadAttribute>();
        HookHelper.Unload();
    }

    public static void Initialize() {
        HookHelper.InitializeAtFirst();
        AttributeUtils.Invoke<InitializeAttribute>();
        typeof(TAS.Manager).GetMethod("DisableRun").HookAfter(() => AttributeUtils.Invoke<TasDisableRunAttribute>());
        typeof(TAS.Manager).GetMethod("EnableRun").HookBefore(() => AttributeUtils.Invoke<TasEnableRunAttribute>());
        GhostModule.Instance.SaveSettings();
        if (Reloading) {
            OnReload();
            Reloading = false;
        } else {
            AttributeUtils.CollectAndSendTasCommand();
        }
    }

    public static void LoadContent() {
        AttributeUtils.Invoke<LoadContentAttribute>();
    }

    public static void OnReload() {
        typeof(TAS.EverestInterop.InfoHUD.InfoCustom).InvokeMethod("CollectAllTypeInfo"); // InfoCustom loses some mod info after hot reload
        AttributeUtils.Invoke<ReloadAttribute>();
    }

    public static bool Reloading;
}