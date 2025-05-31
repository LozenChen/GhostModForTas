using Celeste.Mod.GhostModForTas.Recorder;
using Celeste.Mod.GhostModForTas.Replayer;
using Celeste.Mod.GhostModForTas.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.Reflection;
using YamlDotNet.Serialization;

namespace Celeste.Mod.GhostModForTas.Module;

public class GhostModuleSettings : EverestModuleSettings {
    public GhostModuleMode Mode = GhostModuleMode.Off; // we don't provide BOTH mode in menu, as i think we don't actually need it in normal tas making 

    public bool REPLAYER_CHECK_STARTING_WITH_SAME_ROOM = false;

    public string DefaultName = "Ghost";

    public string PlayerName = "Player";
    public void OnLoadSettings() {
        LastManuallyConfigShowCustomInfo = ShowCustomInfo;
        LastManuallyConfigShowHudInfo = ShowHudInfo;
        ShowInfoEnabler = ShowHudInfo | ShowCustomInfo;
        ComparerAlpha = ComparerOpacity / 10f;
    }

    public bool ForceSync = false;

    public bool IsIGT = true;

    public bool CompareStyleIsModern = true;

    public Alignments ComparerAlignment = Alignments.TopRight;

    public int ComparerOpacity = 10;

    [YamlIgnore]
    public float ComparerAlpha = 1f;

    public bool CompareRoomTime = true;

    public bool CompareTotalTime = true;

    [YamlIgnore]
    public bool ComparerToggler = true;
    public bool ShowCompareTime => ComparerToggler && (CompareRoomTime || CompareTotalTime);

    public bool ShowGhostSprite = true;

    public bool ShowGhostHitbox = true;

    [YamlIgnore]
    public bool LastManuallyConfigShowHudInfo;

    [YamlIgnore]
    public bool LastManuallyConfigShowCustomInfo;

    [YamlIgnore]
    public bool ShowInfoEnabler = true;

    public bool ShowHudInfo = true;
    public bool ShowCustomInfo = false;

    public bool ShowInfo => ShowHudInfo || ShowCustomInfo;


    public string CustomInfoTemplate = "\nEdit your CustomInfo here,\ne.g. {Player.Speed:}";

    public PlayerSpriteMode GhostSpriteMode = PlayerSpriteMode.Madeline;

    public enum ShowInPauseMenuModes { Always, WhenNotInTas, Never }

    public ShowInPauseMenuModes ShowInPauseMenuMode = ShowInPauseMenuModes.WhenNotInTas;

    public Color HitboxColor = defaultHitboxColor;

    [YamlIgnore]
    public static readonly Color defaultHitboxColor = new Color(1f, 0f, 0f, 0.2f);

    public Color HurtboxColor = defaultHurtboxColor;

    [YamlIgnore]
    public static readonly Color defaultHurtboxColor = new Color(0f, 1f, 0f, 0.2f);

    public Vector2 HudInfoPosition = new Vector2(280f, 20f);

    public bool RandomizeGhostColors = true;

    public bool ShowGhostName = false;

    public bool ShowRecorderIcon = true;

    public TimeFormats TimeFormat = TimeFormats.SecondAndFrame;


    [SettingName("GHOST_MOD_FOR_TAS_MAIN_SWITCH_HOTKEY")]
    [SettingSubHeader("GHOST_MOD_FOR_TAS_HOTKEY_DESCRIPTION")]
    [DefaultButtonBinding(new Buttons[] { }, new Keys[] { Keys.LeftControl, Keys.H })]
    public ButtonBinding keyMainSwitch { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.H);

    [SettingName("GHOST_MOD_FOR_TAS_HITBOX_HOTKEY")]
    [DefaultButtonBinding(new Buttons[] { }, new Keys[] { Keys.LeftControl, Keys.J })]
    public ButtonBinding keyGhostHitbox { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.J);

    [SettingName("GHOST_MOD_FOR_TAS_INFO_HUD_HOTKEY")]
    [DefaultButtonBinding(new Buttons[] { }, new Keys[] { Keys.LeftControl, Keys.K })]
    public ButtonBinding keyInfoHud { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.K);

    [SettingName("GHOST_MOD_FOR_TAS_TOGGLE_COMPARER_HOTKEY")]
    [DefaultButtonBinding(0, Keys.Tab)]
    public ButtonBinding keyToggleComparer { get; set; } = new((Buttons)0, Keys.Tab);

#pragma warning disable CS8524
    public bool SettingsHotkeysPressed() {
        if (Engine.Scene is not Level) {
            return false;
        }

        GhostHotkey.Update(true, true);
        bool changed = false; // if settings need to be saved

        if (GhostHotkey.MainSwitchHotkey.Pressed) {
            changed = true;
            if (GhostRecorder.origMode.HasValue) {
                GhostRecorder.StopRecordingCommand();
            } else {
                Mode = Mode switch {
                    GhostModuleMode.Off => GhostModuleMode.Record,
                    GhostModuleMode.Record => GhostModuleMode.Play,
                    GhostModuleMode.Play => GhostModuleMode.Off, // yeah, i think that we don't even actually need BOTH mode
                    GhostModuleMode.Both => GhostModuleMode.Off
                };
                if (Mode == GhostModuleMode.Record && Engine.Scene is Level level && (GhostRecorder.Recorder is null || GhostRecorder.Recorder.Scene != level)) {
                    level.OnEndOfFrame += GhostRecorder.CreateNewRecorderOnEndOfFrame; // in case we pressed the hotkey accidentally
                }
            }
            UpdateStateText();
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
        if (GhostHotkey.ToggleComparerHotkey.Pressed && !Engine.Commands.Open) {
            ComparerToggler = !ComparerToggler;
        }
        return changed;
    }

    private static MethodInfo method = null;

    private static void Refresh(string text) {
        method.Invoke(null, new object[] { text });
    }

    public void UpdateStateText() {
        Refresh("GhostMod Mode = " + Mode switch {
            GhostModuleMode.Off => "Off",
            GhostModuleMode.Record => "Record",
            GhostModuleMode.Play => "Play",
            GhostModuleMode.Both => "Both"
        });
    }
#pragma warning restore CS8524

    [Initialize]
    private static void Initialize() {
        method = ModUtils.GetType("TASHelper", "Celeste.Mod.TASHelper.Entities.HotkeyWatcher")?.GetMethodInfo("Refresh") ?? typeof(GhostHotkeyWatcher).GetMethodInfo("Refresh");
    }

    [Monocle.Command("ghost_record", "[GhostModForTas] Switch to RECORD mode")]
    public static void SwtichToRecordConsoleCommand() {
        if (Engine.Scene is Level level && (!ghostSettings.Mode.HasFlag(GhostModuleMode.Record) || GhostRecorder.Recorder is null || GhostRecorder.Recorder.Scene != level)) {
            GhostRecorder.CreateNewRecorder(level);
        }
        ghostSettings.Mode = GhostModuleMode.Record;
        GhostReplayer.Clear(false);
        RecordingIcon.Instance?.Update();
        ghostSettings.UpdateStateText();
    }

    [Monocle.Command("ghost_play", "[GhostModForTas] Switch to PLAY mode")]
    public static void SwtichToPlayConsoleCommand() {
        ghostSettings.Mode = GhostModuleMode.Play;
        RecordingIcon.Instance?.Update();
        ghostSettings.UpdateStateText();
    }

    [Monocle.Command("ghost_off", "[GhostModForTas] Switch to OFF mode")]
    public static void SwtichToOffConsoleCommand() {
        ghostSettings.Mode = GhostModuleMode.Off;
        GhostReplayer.Clear(false);
        RecordingIcon.Instance?.Update();
        ghostSettings.UpdateStateText();
    }
}

[Flags]
public enum GhostModuleMode {
    Off = 0,
    Record = 1 << 0,
    Play = 1 << 1,
    Both = Record | Play
}

public enum TimeFormats { SecondAndFrame, SecondOnly, FrameOnly };

public enum Alignments { TopLeft, TopRight };