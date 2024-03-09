using Celeste.Mod.GhostModForTas.Entities;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.IO;
using TAS.EverestInterop;
using YamlDotNet.Serialization;

namespace Celeste.Mod.GhostModForTas.Module;

public class GhostModuleSettings : EverestModuleSettings {
    [SettingIgnore] public bool AlwaysShowSettings { get; set; } = false;

    public GhostModuleMode Mode { get; set; } = GhostModuleMode.Both;

    [SettingInGame(false)] public string Name { get; set; } = "Ghost";

    public bool ShowNames { get; set; } = true;


    [SettingIgnore]

    public bool ShowCompareTime => CompareRoomTime || CompareTotalTime;
    public bool CompareRoomTime { get; set; } = true;

    public bool CompareTotalTime { get; set; } = true;

    public bool ForceSync { get; set; } = false;
    public bool HighlightFastestGhost { get; set; } = true;

    [SettingRange(0, 10)] public int InnerOpacity { get; set; } = 10;
    [YamlIgnore][SettingIgnore] public float InnerOpacityFactor => InnerOpacity / 10f;

    [SettingRange(0, 10)] public int InnerHairOpacity { get; set; } = 10;
    [YamlIgnore][SettingIgnore] public float InnerHairOpacityFactor => InnerHairOpacity / 10f;

    [SettingRange(0, 10)] public int OuterOpacity { get; set; } = 10;
    [YamlIgnore][SettingIgnore] public float OuterOpacityFactor => OuterOpacity / 10f;

    [SettingRange(0, 10)] public int OuterHairOpacity { get; set; } = 10;
    [YamlIgnore][SettingIgnore] public float OuterHairOpacityFactor => OuterHairOpacity / 10f;

    [SettingRange(0, 10)] public int InnerRadius { get; set; } = 10;
    [YamlIgnore][SettingIgnore] public float InnerRadiusDist => InnerRadius * InnerRadius * 64f;

    [SettingRange(0, 10)] public int BorderSize { get; set; } = 10;
    [YamlIgnore][SettingIgnore] public float BorderSizeDist => BorderSize * BorderSize * 64f;

    [YamlIgnore] public string ClearAllRecords => "Clear All Records";

    public void CreateClearAllRecordsEntry(TextMenu textMenu, bool inGame) {
        textMenu.Add(new TextMenu.Button("Clear All Records").Pressed(() => {
            if (!Directory.Exists(PathGhosts)) {
                Audio.Play(SFX.ui_main_button_invalid);
                return;
            }

            Audio.Play(SFX.ui_main_button_select);

            DirectoryInfo ghostDir = new DirectoryInfo(PathGhosts);
            foreach (FileInfo file in ghostDir.GetFiles("*" + Recorder.Data.GhostData.OshiroPostfix)) {
                file.Delete();
            }

            GhostCompare.ResetCompareTime();
            GhostReplayer.Replayer?.RemoveSelf();
            GhostReplayer.Replayer = null;
        }));
    }


    [SettingName("GHOST_MAIN_SWITCH_HOTKEY")]
    [SettingSubHeader("GHOST_HOTKEY_DESCRIPTION")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.H)]
    public ButtonBinding keyMainSwitch { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.H);

    public bool MainEnabled = true;

    public bool ShowInPauseMenu = true;

    public bool SettingsHotkeysPressed() {
        if (Engine.Scene is not Level) {
            return false;
        }

        GhostHotkey.Update(true, true);
#pragma warning disable CS8524
        bool changed = false; // if settings need to be saved

        if (GhostHotkey.MainSwitchHotkey.Pressed) {
            changed = true;
            Mode = Mode switch {
                GhostModuleMode.Off => GhostModuleMode.Record,
                GhostModuleMode.Record => GhostModuleMode.Play,
                GhostModuleMode.Play => GhostModuleMode.Both,
                GhostModuleMode.Both => GhostModuleMode.Off
            };
            Refresh("GhostMod Mode = " + Mode switch {
                GhostModuleMode.Off => "Off",
                GhostModuleMode.Record => "Record",
                GhostModuleMode.Play => "Play",
                GhostModuleMode.Both => "Both"
            });
        }
#pragma warning restore CS8524
        return changed;

        static void Refresh(string text) {
            TASHelper.Entities.HotkeyWatcher.Refresh(text);
        }
    }

}

[Flags]
public enum GhostModuleMode {
    Off = 0,
    Record = 1 << 0,
    Play = 1 << 1,
    Both = Record | Play
}