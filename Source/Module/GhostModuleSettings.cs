using Celeste.Mod.GhostModForTas.Recorder;
using Celeste.Mod.GhostModForTas.Replayer;
using Celeste.Mod.GhostModForTas.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.IO;
using System.Reflection;
using TAS.EverestInterop;
using YamlDotNet.Serialization;

namespace Celeste.Mod.GhostModForTas.Module;

public class GhostModuleSettings : EverestModuleSettings {
    public GhostModuleMode Mode = GhostModuleMode.Off; // we don't provide BOTH mode in menu, as i think we don't actually need it in normal tas making 

    public string Name = "Ghost";

    public bool ForceSync = false;

    public bool IsIGT = true;

    public bool CompareRoomTime = true;

    public bool CompareTotalTime = true;

    public bool ShowCompareTime => CompareRoomTime || CompareTotalTime;

    public bool ShowGhostHitbox = true;

    [YamlIgnore]
    public bool LastManuallyConfigShowHudInfo;

    [YamlIgnore]
    public bool LastManuallyConfigShowCustomInfo;

    [YamlIgnore]
    public bool ShowInfoEnabler = true;

    public bool ShowHudInfo = true;
    public bool ShowCustomInfo = true;

    public bool ShowInfo => ShowHudInfo || ShowCustomInfo;


    public string CustomInfoTemplate = "\nEdit your CustomInfo here,\ne.g. {Player.Speed:}";

    public PlayerSpriteMode GhostSpriteMode = PlayerSpriteMode.Madeline;

    public bool ShowInPauseMenu = true;


    public Color HitboxColor = defaultHitboxColor;

    [YamlIgnore]
    public static readonly Color defaultHitboxColor = new Color(1f, 0f, 0f, 0.2f);

    public Color HurtboxColor = defaultHurtboxColor;

    [YamlIgnore]
    public static readonly Color defaultHurtboxColor = new Color(0f, 1f, 0f, 0.2f);

    public Vector2 InfoPosition = new Vector2(280f, 20f);

    public bool ShowRecorderIcon = true;

    public static void CreateClearAllRecordsEntry(TextMenu textMenu) {
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


    [SettingName("GHOST_MOD_FOR_TAS_MAIN_SWITCH_HOTKEY")]
    [SettingSubHeader("GHOST_MOD_FOR_TAS_HOTKEY_DESCRIPTION")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.H)]
    public ButtonBinding keyMainSwitch { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.H);

    [SettingName("GHOST_MOD_FOR_TAS_HITBOX_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.J)]
    public ButtonBinding keyGhostHitbox { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.J);

    [SettingName("GHOST_MOD_FOR_TAS_INFO_HUD_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.K)]
    public ButtonBinding keyInfoHud { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.K);
    public bool SettingsHotkeysPressed() {
        if (Engine.Scene is not Level) {
            return false;
        }

        GhostHotkey.Update(true, true);
#pragma warning disable CS8524
        bool changed = false; // if settings need to be saved

        if (GhostHotkey.MainSwitchHotkey.Pressed) {
            changed = true;
            if (GhostRecorder.origMode.HasValue) {
                GhostRecorder.StopRecordingCommand();
            }
            else {
                Mode = Mode switch {
                    GhostModuleMode.Off => GhostModuleMode.Record,
                    GhostModuleMode.Record => GhostModuleMode.Play,
                    GhostModuleMode.Play => GhostModuleMode.Off, // yeah, i think that we don't even actually need BOTH mode
                    GhostModuleMode.Both => GhostModuleMode.Off
                };
            }
            Refresh("GhostMod Mode = " + Mode switch {
                GhostModuleMode.Off => "Off",
                GhostModuleMode.Record => "Record",
                GhostModuleMode.Play => "Play",
                GhostModuleMode.Both => "Both"
            });
        } else if (GhostHotkey.InfoHudHotkey.Pressed) {
            changed = false;
            if (ShowInfoEnabler) {
                ShowInfoEnabler = false;
                ShowHudInfo = false;
                ShowCustomInfo = false;
            } else {
                ShowInfoEnabler = true;
                ShowHudInfo = LastManuallyConfigShowHudInfo;
                ShowCustomInfo = LastManuallyConfigShowCustomInfo;
                if (!ShowInfo) {
                    ShowHudInfo = LastManuallyConfigShowHudInfo = true;
                    ShowCustomInfo = LastManuallyConfigShowCustomInfo = true;
                }
            }
        } else if (GhostHotkey.GhostHitboxHotkey.Pressed) {
            changed = true;
            ShowGhostHitbox = !ShowGhostHitbox;
        }
#pragma warning restore CS8524
        return changed;

        static void Refresh(string text) {
            method.Invoke(null, new object[] { text });
        }
    }

    private static MethodInfo method = null;

    [Initialize]
    private static void Initialize() {
        method = ModUtils.GetType("TASHelper", "Celeste.Mod.TASHelper.Entities.HotkeyWatcher")?.GetMethodInfo("Refresh") ?? typeof(GhostHotkeyWatcher).GetMethodInfo("Refresh");
    }
}

[Flags]
public enum GhostModuleMode {
    Off = 0,
    Record = 1 << 0,
    Play = 1 << 1,
    Both = Record | Play
}