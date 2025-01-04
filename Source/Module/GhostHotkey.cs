using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;

using Hotkey = Celeste.Mod.GhostModForTas.Module.Hotkeys_BASE.Hotkey_BASE;

namespace Celeste.Mod.GhostModForTas.Module;

public static class GhostHotkey {

    public static Hotkey MainSwitchHotkey { get; set; }

    public static Hotkey InfoHudHotkey { get; set; }

    public static Hotkey GhostHitboxHotkey { get; set; }

    public static Hotkey ToggleComparerHotkey { get; set; }

    public static List<Hotkey> Hotkeys = new();

    [Load]
    public static void Load() {
        On.Celeste.Level.Render += HotkeysPressed;
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Level.Render -= HotkeysPressed;
    }


    [Initialize]
    public static void HotkeyInitialize() {
        MainSwitchHotkey = BindingToHotkey(ghostSettings.keyMainSwitch);
        GhostHitboxHotkey = BindingToHotkey(ghostSettings.keyGhostHitbox);
        InfoHudHotkey = BindingToHotkey(ghostSettings.keyInfoHud);
        ToggleComparerHotkey = BindingToHotkey(ghostSettings.keyToggleComparer);
        Hotkeys = new List<Hotkey> { MainSwitchHotkey, GhostHitboxHotkey, InfoHudHotkey, ToggleComparerHotkey };
    }

    private static void HotkeysPressed(On.Celeste.Level.orig_Render orig, Level self) {
        orig(self);
        if (ghostSettings.SettingsHotkeysPressed()) {
            GhostModule.Instance.SaveSettings();
        }
    }

    public static void Update(bool updateKey, bool updateButton) {
        Hotkeys_BASE.UpdateMeta();
        foreach (Hotkey hotkey in Hotkeys) {
            hotkey.Update(updateKey, updateButton);
        }
    }

    private static Hotkey BindingToHotkey(ButtonBinding binding, bool held = false) {
        return new(binding.Keys, binding.Buttons, true, held);
    }
}



// taken from CelesteTAS
public static class Hotkeys_BASE {

    private static KeyboardState kbState;
    private static GamePadState padState;

    internal static void UpdateMeta() {
        kbState = Keyboard.GetState();
        padState = GetGamePadState();

    }
    private static GamePadState GetGamePadState() {
        GamePadState currentState = MInput.GamePads[0].CurrentState;
        for (int i = 0; i < 4; i++) {
            currentState = GamePad.GetState((PlayerIndex)i);
            if (currentState.IsConnected) {
                break;
            }
        }

        return currentState;
    }

    public class Hotkey_BASE {
        public readonly List<Buttons> Buttons;
        private readonly bool held;
        private readonly bool keyCombo;
        public readonly List<Keys> Keys;
        private DateTime lastPressedTime;
        public bool OverrideCheck;

        public Hotkey_BASE(List<Keys> keys, List<Buttons> buttons, bool keyCombo, bool held) {
            Keys = keys;
            Buttons = buttons;
            this.keyCombo = keyCombo;
            this.held = held;
        }

        public bool Check { get; private set; }
        public bool LastCheck { get; private set; }
        public bool Pressed => !LastCheck && Check;

        // note: dont check DoublePressed on render, since unstable DoublePressed response during frame drops
        public bool DoublePressed { get; private set; }
        public bool Released => LastCheck && !Check;

        public void Update(bool updateKey = true, bool updateButton = true) {
            LastCheck = Check;
            bool keyCheck;
            bool buttonCheck;

            if (OverrideCheck) {
                keyCheck = buttonCheck = true;
                if (!held) {
                    OverrideCheck = false;
                }
            } else {
                keyCheck = updateKey && IsKeyDown();
                buttonCheck = updateButton && IsButtonDown();
            }

            Check = keyCheck || buttonCheck;

            if (Pressed) {
                DateTime pressedTime = DateTime.Now;
                DoublePressed = pressedTime.Subtract(lastPressedTime).TotalMilliseconds < 200;
                lastPressedTime = DoublePressed ? default : pressedTime;
            } else {
                DoublePressed = false;
            }
        }

        private bool IsKeyDown() {
            if (Keys == null || Keys.Count == 0 || kbState == default) {
                return false;
            }

            return keyCombo ? Keys.All(kbState.IsKeyDown) : Keys.Any(kbState.IsKeyDown);
        }

        private bool IsButtonDown() {
            if (Buttons == null || Buttons.Count == 0 || padState == default) {
                return false;
            }

            return keyCombo ? Buttons.All(padState.IsButtonDown) : Buttons.Any(padState.IsButtonDown);
        }
    }
}