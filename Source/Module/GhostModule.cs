using System;

namespace Celeste.Mod.GhostModForTas.Module;

public class GhostModule : EverestModule {
    public static GhostModule Instance;

    public override Type SettingsType => typeof(GhostModuleSettings);
    public static GhostModuleSettings ModuleSettings => (GhostModuleSettings)Instance._Settings;


    public GhostModule() {
        Instance = this;
        AttributeUtils.CollectMethods<LoadAttribute>();
        AttributeUtils.CollectMethods<UnloadAttribute>();
        AttributeUtils.CollectMethods<InitializeAttribute>();
        AttributeUtils.CollectMethods<FreezeUpdateAttribute>();
        AttributeUtils.CollectMethods<SkippingCutsceneUpdateAttribute>();
        AttributeUtils.CollectMethods<UnpauseUpdateAttribute>();
    }


    public override void Load() {
        Loader.Load();
    }

    public override void Unload() {
        if (!ghostSettings.ShowInfoEnabler) {
            ghostSettings.ShowHudInfo = ghostSettings.LastManuallyConfigShowHudInfo;
            ghostSettings.ShowCustomInfo = ghostSettings.LastManuallyConfigShowCustomInfo;
            SaveSettings();
        }

        Loader.Unload();
    }

    public override void Initialize() {
        Loader.Initialize();
    }

    public override void LoadContent(bool firstLoad) {
        if (firstLoad) {
            Loader.LoadContent();
        }
    }

    public override void LoadSettings() {
        base.LoadSettings();
        ghostSettings.OnLoadSettings();
    }

    public override void CreateModMenuSection(TextMenu menu, bool inGame, FMOD.Studio.EventInstance snapshot) {
        CreateModMenuSectionHeader(menu, inGame, snapshot);
        GhostModMenu.CreateMenu(this, menu, inGame, false);
    }
}