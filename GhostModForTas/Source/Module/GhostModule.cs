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
        AttributeUtils.CollectMethods<LoadContentAttribute>();
        AttributeUtils.CollectMethods<InitializeAttribute>();
        AttributeUtils.CollectMethods<TasDisableRunAttribute>();
        AttributeUtils.CollectMethods<TasEnableRunAttribute>();
        AttributeUtils.CollectMethods<ReloadAttribute>();
    }


    public override void Load() {
        Loader.Load();
    }

    public override void Unload() {
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

    public override void CreateModMenuSection(TextMenu menu, bool inGame, FMOD.Studio.EventInstance snapshot) {
        base.CreateModMenuSection(menu, inGame, snapshot);
        /*
        CreateModMenuSectionHeader(menu, inGame, snapshot);
        menu.Add(new TextMenuExt.SubHeaderExt("") { HeightExtra = 2f });
        ModOptionsMenu.CreateMenu(this, menu, inGame, false);
        */
    }
}