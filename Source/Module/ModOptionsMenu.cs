using Celeste.Mod.GhostModForTas.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;
using System.Reflection;
using static Celeste.TextMenu;

namespace Celeste.Mod.GhostModForTas.Module;

internal static class ModOptionsMenu {

    public static void CreateMenu(EverestModule everestModule, TextMenu menu, bool inGame, bool inPauseMenu = false) {
        menu.Add(new TextMenuExt.EnumerableSlider<GhostModuleMode>("Mode".ToDialogText(), CreateModeOptions(),
        ghostSettings.Mode).Change(value => ghostSettings.Mode = value));

        menu.Add(new TextMenu.OnOff("Force Sync".ToDialogText(), ghostSettings.ForceSync).Change(value => ghostSettings.ForceSync = value));
        menu.Add(new TextMenu.OnOff("Compare Room Time".ToDialogText(), ghostSettings.CompareRoomTime).Change(value => ghostSettings.CompareRoomTime = value));
        menu.Add(new TextMenu.OnOff("Compare Total Time".ToDialogText(), ghostSettings.CompareTotalTime).Change(value => ghostSettings.CompareTotalTime = value));
        menu.Add(new TextMenu.OnOff("Show Ghost Hitbox".ToDialogText(), ghostSettings.ShowGhostHitbox).Change(value => ghostSettings.ShowGhostHitbox = value));
        menu.Add(new TextMenu.OnOff("Show HUD Info".ToDialogText(), ghostSettings.ShowHudInfo).Change(value => ghostSettings.ShowHudInfo = value));
        // menu.Add(new TextMenu.OnOff("Show Custom Info".ToDialogText(), ghostSettings.UseCustomInfo).Change(value => ghostSettings.UseCustomInfo = value));
        menu.Add(new TextMenu.Button("Info Copy Custom Template".ToTASDialogText()).Pressed(() =>
                TextInput.SetClipboardText(string.IsNullOrEmpty(ghostSettings.CustomInfoTemplate) ? "\0" : ghostSettings.CustomInfoTemplate)));
        menu.Add(new TextMenu.Button("Info Set Custom Template".ToTASDialogText()).Pressed(() => {
            ghostSettings.CustomInfoTemplate = TextInput.GetClipboardText() ?? string.Empty;
            GhostModule.Instance.SaveSettings();
        }));
        menu.Add(new TextMenu.OnOff("Show in Pause Menu".ToDialogText(), ghostSettings.ShowInPauseMenu).Change(value => ghostSettings.ShowInPauseMenu = value));
        GhostModuleSettings.CreateClearAllRecordsEntry(menu);
        menu.Add(new TextMenu.Button(Dialog.Clean("options_keyconfig")).Pressed(() => {
            menu.Focused = false;
            KeyboardConfigUI keyboardConfig;
            if (CreateKeyboardConfigUi != null) {
                keyboardConfig = (KeyboardConfigUI)CreateKeyboardConfigUi.Invoke(everestModule, new object[] { menu });
            } else {
                keyboardConfig = new ModuleSettingsKeyboardConfigUI(everestModule);
            }

            keyboardConfig.OnClose = () => { menu.Focused = true; };

            Engine.Scene.Add(keyboardConfig);
            Engine.Scene.OnEndOfFrame += () => Engine.Scene.Entities.UpdateLists();
        }));

        menu.Add(new TextMenu.Button(Dialog.Clean("options_btnconfig")).Pressed(() => {
            menu.Focused = false;
            ButtonConfigUI buttonConfig;
            if (CreateButtonConfigUI != null) {
                buttonConfig = (ButtonConfigUI)CreateButtonConfigUI.Invoke(everestModule, new object[] { menu });
            } else {
                buttonConfig = new ModuleSettingsButtonConfigUI(everestModule);
            }

            buttonConfig.OnClose = () => { menu.Focused = true; };

            Engine.Scene.Add(buttonConfig);
            Engine.Scene.OnEndOfFrame += () => Engine.Scene.Entities.UpdateLists();
        }));
    }

    private static readonly MethodInfo CreateKeyboardConfigUi = typeof(EverestModule).GetMethodInfo("CreateKeyboardConfigUI");
    private static readonly MethodInfo CreateButtonConfigUI = typeof(EverestModule).GetMethodInfo("CreateButtonConfigUI");
    internal static string ToTASDialogText(this string input) => Dialog.Clean("TAS_" + input.Replace(" ", "_"));

    private static IEnumerable<KeyValuePair<GhostModuleMode, string>> CreateModeOptions() {
        return new List<KeyValuePair<GhostModuleMode, string>> {
            new(GhostModuleMode.Off, "Mode Off".ToDialogText()),
            new(GhostModuleMode.Record, "Mode Record".ToDialogText()),
            new(GhostModuleMode.Play, "Mode Play".ToDialogText()),
            new(GhostModuleMode.Both, "Mode Both".ToDialogText()),
        };
    }
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
        menu.Add(new HeaderExt("Ghost Title".ToDialogText(), Color.Silver, Color.Black));
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
}


public static class DialogExtension {
    internal static string ToDialogText(this string input) => Dialog.Clean("GHOST_MOD_FOR_TAS_" + input.ToUpper().Replace(" ", "_"));
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
