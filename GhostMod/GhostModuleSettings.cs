using System;
using System.IO;
using YamlDotNet.Serialization;

namespace Celeste.Mod.Ghost;

public class GhostModuleSettings : EverestModuleSettings {
    [SettingIgnore] public bool AlwaysShowSettings { get; set; } = false;

    public GhostModuleMode Mode { get; set; } = GhostModuleMode.Both;

    [SettingInGame(false)] public string Name { get; set; } = "Ghost";

    [SettingIgnore] // Ignore on older builds of Everest which don't support custom entry creators.
    public string NameFilter { get; set; } = "";
    
    public bool ShowNames { get; set; } = true;

    public bool ShowDeaths { get; set; } = true;

    [SettingIgnore]

    public bool ShowCompareTime => CompareRoomTime || CompareTotalTime;
    public bool CompareRoomTime { get; set; } = true;

    public bool CompareTotalTime { get; set; } = true; 

    public bool HighlightFastestGhost { get; set; } = true;

    public bool ReversedPlayerSpriteMode { get; set; } = true;

    [SettingRange(0, 10)] public int InnerOpacity { get; set; } = 10;
    [YamlIgnore] [SettingIgnore] public float InnerOpacityFactor => InnerOpacity / 10f;

    [SettingRange(0, 10)] public int InnerHairOpacity { get; set; } = 10;
    [YamlIgnore] [SettingIgnore] public float InnerHairOpacityFactor => InnerHairOpacity / 10f;

    [SettingRange(0, 10)] public int OuterOpacity { get; set; } = 10;
    [YamlIgnore] [SettingIgnore] public float OuterOpacityFactor => OuterOpacity / 10f;

    [SettingRange(0, 10)] public int OuterHairOpacity { get; set; } = 10;
    [YamlIgnore] [SettingIgnore] public float OuterHairOpacityFactor => OuterHairOpacity / 10f;

    [SettingRange(0, 10)] public int InnerRadius { get; set; } = 10;
    [YamlIgnore] [SettingIgnore] public float InnerRadiusDist => InnerRadius * InnerRadius * 64f;

    [SettingRange(0, 10)] public int BorderSize { get; set; } = 10;
    [YamlIgnore] [SettingIgnore] public float BorderSizeDist => BorderSize * BorderSize * 64f;

    [YamlIgnore] public string ClearAllRecords => "Clear All Records";
    // EverestModule.CreateModMenuSection(...) will automatically allocate "Create XXX Entry" to that "XXX" property. So do not delete this.
    public void ShowNameFilterEntry(TextMenu menu, bool inGame) {
        // TODO: Create a slider to choose between all available names.
    }

    public void CreateClearAllRecordsEntry(TextMenu textMenu, bool inGame) {
        textMenu.Add(new TextMenu.Button("Clear All Records").Pressed(() => {
            if (!Directory.Exists(GhostModule.PathGhosts)) {
                Audio.Play(SFX.ui_main_button_invalid);
                return;
            }

            Audio.Play(SFX.ui_main_button_select);

            DirectoryInfo ghostDir = new DirectoryInfo(GhostModule.PathGhosts);
            foreach (FileInfo file in ghostDir.GetFiles()) {
                file.Delete();
            }
        }));
    }
}

[Flags]
public enum GhostModuleMode {
    Off = 0,
    Record = 1 << 0,
    Play = 1 << 1,
    Both = Record | Play
}