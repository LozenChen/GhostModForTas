using Celeste.Mod.GhostModForTas.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Reflection;
using static Celeste.TextMenu;

namespace Celeste.Mod.GhostModForTas.Module;

internal static class ModOptionsMenu {

    private static TextMenu.Item indexer;
    public static void CreateMenu(EverestModule everestModule, TextMenu menu, bool inGame, bool inPauseMenu = false) {
        TextMenu.Item mainItem = new OnOff("Enabled".ToDialogText(), ghostSettings.MainEnabled).Change(value => { ghostSettings.MainEnabled = value; UpdateEnableItems(value, true, everestModule, menu, inGame); });
        TextMenu.Item showInPauseMenu = new OnOff("Show In Pause Menu".ToDialogText(), ghostSettings.ShowInPauseMenu).Change(value => {
            ghostSettings.ShowInPauseMenu = value;
            HookPauseMenu.OnShowInPauseMenuChange(value);
        });
        if (inPauseMenu) {
            menu.Add(new TextMenuExt.SubHeaderExt("") { HeightExtra = 20f });
            menu.Add(indexer = mainItem);
            UpdateEnableItems(ghostSettings.MainEnabled, false, everestModule, menu, inGame);
            menu.Add(new TextMenuExt.SubHeaderExt("") { HeightExtra = 40f });
            menu.Add(showInPauseMenu);
        } else {
            menu.Add(mainItem);
            menu.Add(indexer = showInPauseMenu);
            UpdateEnableItems(ghostSettings.MainEnabled, false, everestModule, menu, inGame);
        }
        UpdateEnableItems(ghostSettings.MainEnabled, false, everestModule, menu, inGame);
        menu.OnClose += () => disabledItems.Clear();
    }

    private static void UpdateEnableItems(bool enable, bool fromChange, EverestModule everestModule, TextMenu menu, bool inGame) {
        if (enable) {
            foreach (TextMenu.Item item in disabledItems) {
                menu.Remove(item);
            }
            disabledItems = new List<TextMenu.Item>();

            Add(new EaseInSubHeader("Sub Header".ToDialogText()) { HeightExtra = 30f });


            /*

            Add(new TextMenu.Button(Dialog.Clean("options_keyconfig")).Pressed(() => {
                menu.Focused = false;
                KeyboardConfigUI keyboardConfig = new ModuleSettingsKeyboardConfigUIExt(everestModule) {
                    OnClose = () => { menu.Focused = true; GhostHotkey.HotkeyInitialize(); }
                };

                Engine.Scene.Add(keyboardConfig);
                Engine.Scene.OnEndOfFrame += () => Engine.Scene.Entities.UpdateLists();
            }));
            Add(new TextMenu.Button(Dialog.Clean("options_btnconfig")).Pressed(() => {
                menu.Focused = false;
                ButtonConfigUI buttonConfig = new ModuleSettingsButtonConfigUI(everestModule) {
                    OnClose = () => { menu.Focused = true; GhostHotkey.HotkeyInitialize(); }
                };

                Engine.Scene.Add(buttonConfig);
                Engine.Scene.OnEndOfFrame += () => Engine.Scene.Entities.UpdateLists();
            }));

            */


            int index = menu.IndexOf(indexer);

            foreach (TextMenu.Item item in disabledItems) {
                index++;
                menu.Insert(index, item);
            }

            foreach (IEaseInItem item in disabledItems) {
                item.Initialize();
            }
        } else {
            foreach (IEaseInItem item in disabledItems) {
                item.FadeVisible = false;
            }
        }

        void Add(TextMenu.Item item) {
            disabledItems.Add(item);
        }
    }

    private static List<TextMenu.Item> disabledItems = new();
}

internal static class HookPauseMenu {
    // basically same as what vanilla variants menu do

    private static TextMenu.Item itemInPauseMenu;

    [Initialize]
    private static void Initialize() {
        using (new DetourContext() { Before = new List<string>() { "*" } }) {
            typeof(Level).GetMethodInfo("Pause").IlHook(il => {
                ILCursor cursor = new ILCursor(il);
                if (cursor.TryGotoNext(ins => ins.MatchCall(typeof(Everest.Events.Level), "CreatePauseMenuButtons"))) {
                    cursor.Index++;
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.Emit(OpCodes.Ldloc_0);
                    cursor.Emit(OpCodes.Ldarg_2);
                    cursor.EmitDelegate(TryAddButton);
                }
            });
        }
        // idk, if using Everest.Events.Level.OnCreatePauseMenuButtons, first i will encounter CS0229, after resolving this i find that my function adds to this event but nothing happens
        // so i go back to ilhook
        // previously i just hook into Celeste.Mod.Everest/Events/Level::CreatePauseMenuButtons
        // but it conflicts with XaphanHelper's on hook on Level.Pause (i.e. my hook just disappear), i've checked that mod's code and found no issue
        // even if i tell the publicizer not to publicize Everest.Events.Level.OnCreatePauseMenuButtons, it does not work, unless i don't publicize Celeste
    }


    private static void TryAddButton(Level level, TextMenu menu, bool minimal) {
        if (minimal || !ghostSettings.ShowInPauseMenu) {
            return;
        }
        // yeah we are after extended variant

        int optionsIndex = menu.Items.FindIndex(item =>
            item.GetType() == typeof(TextMenu.Button) && ((TextMenu.Button)item).Label == Dialog.Clean("menu_pause_options"));

        menu.Insert(optionsIndex, itemInPauseMenu = new TextMenu.Button("Ghost Module Menu".ToDialogText()).Pressed(() => {
            menu.RemoveSelf();
            level.PauseMainMenuOpen = false;
            level.CreateMenuInPause(menu.IndexOf(itemInPauseMenu));
        }));
    }

    private static void CreateMenuInPause(this Level level, int returnIndex) {
        level.Paused = true;
        TextMenu menu = new TextMenu();
        menu.Add(new HeaderExt("CU Variant Title".ToDialogText(), Color.Silver, Color.Black));
        ModOptionsMenu.CreateMenu(GhostModule.Instance, menu, true, true);

        menu.OnESC = menu.OnCancel = () => {
            Audio.Play("event:/ui/main/button_back");
            GhostModule.Instance.SaveSettings();
            level.Pause(returnIndex, false);
            menu.Close();
        };
        menu.OnPause = () => {
            Audio.Play("event:/ui/main/button_back");
            GhostModule.Instance.SaveSettings();
            level.Paused = false;
            level.unpauseTimer = 0.15f;
            menu.Close();
        };
        level.Add(menu);
    }

    internal static void OnShowInPauseMenuChange(bool visible) {
        if (itemInPauseMenu?.Container is { }) {
            itemInPauseMenu.Visible = visible;
        }
    }
}


public static class DialogExtension {
    internal static string ToDialogText(this string input) => Dialog.Clean("CEILING_ULTRA_" + input.ToUpper().Replace(" ", "_"));
}

public interface IEaseInItem {
    public void Initialize();
    public bool FadeVisible { get; set; }
}

public class HeaderExt : Item {
    public const float Scale = 2f;

    public string Title;

    public Color TextColor;

    public Color StrokeColor;
    public HeaderExt(string title, Color text, Color stroke) {
        Title = title;
        Selectable = false;
        IncludeWidthInMeasurement = false;
        TextColor = text;
        StrokeColor = stroke;
    }

    public override float LeftWidth() {
        return ActiveFont.Measure(Title).X * 2f;
    }

    public override float Height() {
        return ActiveFont.LineHeight * 2f;
    }

    public override void Render(Vector2 position, bool highlighted) {
        float alpha = Container.Alpha;
        ActiveFont.DrawEdgeOutline(Title, position + new Vector2(Container.Width * 0.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.One * 2f, TextColor * alpha, 4f, Color.DarkSlateBlue * alpha, 2f, StrokeColor * (alpha * alpha * alpha));
    }
}

public class EaseInSubHeader : TextMenuExt.SubHeaderExt, IEaseInItem {
    private float alpha;
    private float unEasedAlpha;

    public void Initialize() {
        alpha = unEasedAlpha = 0f;
        Visible = FadeVisible = true;
    }
    public bool FadeVisible { get; set; }
    public EaseInSubHeader(string label) : base(label) {
        alpha = unEasedAlpha = 1f;
        FadeVisible = Visible = true;
    }

    public override float Height() => MathHelper.Lerp(-Container.ItemSpacing, base.Height(), alpha);

    public override void Update() {
        base.Update();

        float targetAlpha = FadeVisible ? 1 : 0;
        if (Math.Abs(unEasedAlpha - targetAlpha) > 0.001f) {
            unEasedAlpha = Calc.Approach(unEasedAlpha, targetAlpha, Engine.RawDeltaTime * 3f);
            alpha = FadeVisible ? Ease.SineOut(unEasedAlpha) : Ease.SineIn(unEasedAlpha);
        }

        Visible = alpha != 0;
    }

    public override void Render(Vector2 position, bool highlighted) {
        float c = Container.Alpha;
        Container.Alpha = alpha;
        base.Render(position, highlighted);
        Container.Alpha = c;
    }
}



[Tracked(false)]
public class ModuleSettingsKeyboardConfigUIExt : ModuleSettingsKeyboardConfigUI {

    public ModuleSettingsKeyboardConfigUIExt(EverestModule module) : base(module) {
    }

    public override void Reload(int index = -1) {
        if (Module == null)
            return;

        Clear();
        Add(new Header(Dialog.Clean("KEY_CONFIG_TITLE")));
        Add(new InputMappingInfo(false));

        object settings = Module._Settings;

        // The default name prefix.
        string typeName = Module.SettingsType.Name.ToLowerInvariant();
        if (typeName.EndsWith("settings"))
            typeName = typeName.Substring(0, typeName.Length - 8);
        string nameDefaultPrefix = $"modoptions_{typeName}_";

        SettingInGameAttribute attribInGame;

        foreach (PropertyInfo prop in Module.SettingsType.GetProperties()) {
            if ((attribInGame = prop.GetCustomAttribute<SettingInGameAttribute>()) != null &&
                attribInGame.InGame != Engine.Scene is Level)
                continue;

            if (prop.GetCustomAttribute<SettingIgnoreAttribute>() != null)
                continue;

            if (!prop.CanRead || !prop.CanWrite)
                continue;

            if (typeof(ButtonBinding).IsAssignableFrom(prop.PropertyType)) {
                if (!(prop.GetValue(settings) is ButtonBinding binding))
                    continue;

                string name = prop.GetCustomAttribute<SettingNameAttribute>()?.Name ?? $"{nameDefaultPrefix}{prop.Name.ToLowerInvariant()}";
                name = name.DialogCleanOrNull() ?? (prop.Name.ToLowerInvariant().StartsWith("button") ? prop.Name.Substring(6) : prop.Name).SpacedPascalCase();

                DefaultButtonBindingAttribute defaults = prop.GetCustomAttribute<DefaultButtonBindingAttribute>();

                Bindings.Add(new ButtonBindingEntry(binding, defaults));

#pragma warning disable CS8600
                string subheader = prop.GetCustomAttribute<SettingSubHeaderAttribute>()?.SubHeader;
#pragma warning restore CS8600
                if (subheader != null)
                    Add(new TextMenuExt.SubHeaderExt(subheader.DialogCleanOrNull() ?? subheader) {
                        TextColor = Color.Gray,
                        Offset = new Vector2(0f, -60f),
                        HeightExtra = 60f
                    });

                AddMapForceLabel(name, binding.Binding);
            }
        }

        Add(new SubHeader(""));
        Add(new Button(Dialog.Clean("KEY_CONFIG_RESET")) {
            IncludeWidthInMeasurement = false,
            AlwaysCenter = true,
            OnPressed = () => ResetPressed()
        });

        if (index >= 0)
            Selection = index;
    }
}