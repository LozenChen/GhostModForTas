using Celeste.Mod.GhostModForTas.Utils;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TAS.EverestInterop;
using Hotkey = TAS.EverestInterop.Hotkeys.Hotkey;

namespace Celeste.Mod.GhostModForTas.Module;

public static class TH_Hotkeys {

    public static Hotkey MainSwitchHotkey { get; set; }

    public static List<Hotkey> Hotkeys = new();

    [Load]
    public static void Load() {
        On.Celeste.Level.Render += HotkeysPressed;
        IL.Celeste.Mod.ModuleSettingsKeyboardConfigUI.Reset += ModReload;
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Level.Render -= HotkeysPressed;
        IL.Celeste.Mod.ModuleSettingsKeyboardConfigUI.Reset -= ModReload;
    }


    [Initialize]
    public static void HotkeyInitialize() {
        MainSwitchHotkey = BindingToHotkey(ghostSettings.keyMainSwitch);

        Hotkeys = new List<Hotkey> { MainSwitchHotkey };
    }

    private static void HotkeysPressed(On.Celeste.Level.orig_Render orig, Level self) {
        orig(self);
        if (GhostModuleSettings.SettingsHotkeysPressed()) {
            GhostModule.Instance.SaveSettings();
        }
    }

    public static void Update(bool updateKey, bool updateButton) {
        foreach (Hotkey hotkey in Hotkeys) {
            hotkey.Update(updateKey, updateButton);
        }
    }

    private static Hotkey BindingToHotkey(ButtonBinding binding, bool held = false) {
        return new(binding.Keys, binding.Buttons, true, held);
    }

    private static IEnumerable<PropertyInfo> bindingProperties;

    private static FieldInfo bindingFieldInfo;

    private static void ModReload(ILContext il) {
        bindingProperties = typeof(GhostModuleSettings)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(info => info.PropertyType == typeof(ButtonBinding) &&
                           info.GetCustomAttribute<DefaultButtonBinding2Attribute>() is { } extraDefaultKeyAttribute &&
                           extraDefaultKeyAttribute.ExtraKey != Keys.None);

        ILCursor ilCursor = new(il);
        if (ilCursor.TryGotoNext(
                MoveType.After,
                ins => ins.OpCode == OpCodes.Callvirt && ins.Operand.ToString().Contains("<Microsoft.Xna.Framework.Input.Keys>::Add(T)")
            )) {
            ilCursor.Emit(OpCodes.Ldloc_1).EmitDelegate(AddExtraDefaultKey);
        }
    }

    private static void AddExtraDefaultKey(object bindingEntry) {
        if (bindingFieldInfo == null) {
            bindingFieldInfo = bindingEntry.GetType().GetFieldInfo("Binding");
        }

        if (bindingFieldInfo?.GetValue(bindingEntry) is not ButtonBinding binding) {
            return;
        }

        if (bindingProperties.FirstOrDefault(info => info.GetValue(ghostSettings) == binding) is { } propertyInfo) {
            binding.Keys.Add(propertyInfo.GetCustomAttribute<DefaultButtonBinding2Attribute>().ExtraKey);
        }
    }
}