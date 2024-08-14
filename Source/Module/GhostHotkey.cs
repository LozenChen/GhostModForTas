using Celeste.Mod.GhostModForTas.Utils;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TAS.EverestInterop;

using Hotkey = TAS.EverestInterop.Hotkeys.Hotkey;

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
        typeof(ModuleSettingsKeyboardConfigUI).GetMethod("Reset").IlHook(ModReload);
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




[Tracked(false)]
public class GhostHotkeyWatcher : Message {

    public static GhostHotkeyWatcher Instance;
    public static float lifetime = 3f;

    public float lifetimer = 0f;
    public GhostHotkeyWatcher() : base("", new Vector2(10f, 1060f)) {
        this.Depth = -20000;
        base.Tag |= Tags.Global;
    }

    public static bool AddIfNecessary() {
        if (Engine.Scene is not Level level) {
            return false;
        }
        if (Instance is null || !level.Entities.Contains(Instance)) {
            Instance = new();
            level.Add(Instance);
        }
        return true;
    }


    private void RefreshImpl(string text) {
        RestoreAlpha(this.text.Equals(text));
        this.text = text;
        lifetimer = lifetime;
        Active = true;
        Visible = true;
    }

    public static void Refresh(string text) {
        if (AddIfNecessary()) {
            Instance.RefreshImpl(text);
        }
    }

    private void RestoreAlpha(bool sameText) {
        if (sameText) {
            FallAndRise = true;
        } else {
            alpha = 1f;
        }
    }

    private bool FallAndRise = false;
    public override void Update() {
        if (FallAndRise) {
            alpha -= 0.1f;
            if (alpha < 0f) {
                alpha = 1f;
                FallAndRise = false;
            }
        } else {
            if (lifetimer / lifetime < 0.1f) {
                alpha = 10 * lifetimer / lifetime;
            }
            lifetimer -= Engine.RawDeltaTime;
            if (lifetimer < 0f) {
                lifetimer = 0f;
                Active = Visible = false;
            }
        }

        base.Update();
    }

    public override void Render() {
        float scale = 0.6f;
        Vector2 Size = FontSize.Measure(text) * scale;
        Monocle.Draw.Rect(Position - 0.5f * Size.Y * Vector2.UnitY - 10f * Vector2.UnitX, Size.X + 20f, Size.Y + 10f, Color.Black * alpha * 0.5f);
        Font.Draw(BaseSize, text, Position, new Vector2(0f, 0.5f), Vector2.One * scale, Color.White * alpha, 0f, Color.Transparent, 1f, Color.Black);
    }

}

[Tracked(false)]
public class Message : Entity {
    internal static readonly Language english = Dialog.Languages["english"];

    internal static readonly PixelFont Font = Fonts.Get(english.FontFace);

    internal static readonly float BaseSize = english.FontFaceSize;

    public static readonly PixelFontSize FontSize = Font.Get(BaseSize);

    public string text;

    public float alpha;

    public Message(string text, Vector2 Position) : base(Position) {
        base.Tag = Tags.HUD;
        this.text = text;
        alpha = 1f;
    }
    public override void Update() {
        base.Update();
    }

    public override void Render() {
        RenderAtCenter(Position);
    }

    public void RenderAtTopLeft(Vector2 Position) {
        Font.Draw(BaseSize, text, Position, new Vector2(0f, 0f), Vector2.One * 0.6f, Color.White * alpha, 0f, Color.Transparent, 1f, Color.Black);
    }

    public void RenderAtCenter(Vector2 Position) {
        Font.Draw(BaseSize, text, Position, new Vector2(0.5f, 0.5f), Vector2.One * 0.5f, Color.White * alpha, 0f, Color.Transparent, 1f, Color.Black);
    }

    public static void RenderMessage(string str, Vector2 Position, Vector2 scale) {
        RenderMessage(str, Position, Vector2.One * 0.5f, scale);
    }

    public static void RenderMessage(string str, Vector2 Position, Vector2 justify, Vector2 scale) {
        Font.DrawOutline(BaseSize, str, Position, justify, scale, Color.White, 2f, Color.Black);
    }
    public static void RenderMessage(string str, Vector2 Position, Vector2 justify, Vector2 scale, float stroke) {
        Font.DrawOutline(BaseSize, str, Position, justify, scale, Color.White, stroke, Color.Black);
    }

    public static void RenderMessage(string str, Vector2 Position, Vector2 justify, Vector2 scale, float stroke, Color colorInside, Color colorOutline) {
        Font.DrawOutline(BaseSize, str, Position, justify, scale, colorInside, stroke, colorOutline);
    }

    public static void RenderMessageJetBrainsMono(string str, Vector2 Position, Vector2 justify, Vector2 scale, float stroke, Color colorInside, Color colorOutline) {
        TAS.EverestInterop.InfoHUD.JetBrainsMonoFont.DrawOutline(str, Position, justify, scale, colorInside, stroke, colorOutline);
    }
}