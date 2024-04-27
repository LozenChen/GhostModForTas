using Celeste.Mod.GhostModForTas.Recorder;
using Celeste.Mod.GhostModForTas.Replayer;
using Celeste.Mod.GhostModForTas.Utils;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Celeste.TextMenu;
using static Celeste.TextMenuExt;
using static TAS.EverestInterop.Hitboxes.HitboxColor;

namespace Celeste.Mod.GhostModForTas.Module;

internal static class ModOptionsMenu {

    public static void CreateMenu(EverestModule everestModule, TextMenu menu, bool inGame, bool inPauseMenu = false) {
        menu.Add(new TextMenuExt.EnumerableSlider<GhostModuleMode>("Mode".ToDialogText(), CreateMainModeOptions(),
        ghostSettings.Mode).Change(value => { ghostSettings.Mode = value; GhostReplayer.Clear(); RecordingIcon.Instance?.Update(); }));

        menu.Add(new TextMenu.OnOff("Force Sync".ToDialogText(), ghostSettings.ForceSync).Change(value => { ghostSettings.ForceSync = value; GhostReplayer.Clear(); }));
        menu.Add(new TextMenuExt.EnumerableSlider<TimeFormats>("Time Format".ToDialogText(), CreateTimeFormatOptions(), ghostSettings.TimeFormat).Change(value => ghostSettings.TimeFormat = value));
        menu.Add(new TextMenuExt.EnumerableSlider<bool>("Timer Mode".ToDialogText(), CreateRTA_IGTOptions(), ghostSettings.IsIGT).Change(value => ghostSettings.IsIGT = value));
        menu.Add(new TextMenu.OnOff("Compare Room Time".ToDialogText(), ghostSettings.CompareRoomTime).Change(value => ghostSettings.CompareRoomTime = value));
        menu.Add(new TextMenu.OnOff("Compare Total Time".ToDialogText(), ghostSettings.CompareTotalTime).Change(value => ghostSettings.CompareTotalTime = value));
        menu.Add(new TextMenu.OnOff("Show Ghost Sprite".ToDialogText(), ghostSettings.ShowGhostSprite).Change(value => ghostSettings.ShowGhostSprite = value));
        menu.Add(new TextMenu.OnOff("Show Ghost Hitbox".ToDialogText(), ghostSettings.ShowGhostHitbox).Change(value => ghostSettings.ShowGhostHitbox = value));
        menu.Add(new TextMenu.OnOff("Show HUD Info".ToDialogText(), ghostSettings.ShowHudInfo).Change(value => { ghostSettings.ShowHudInfo = value; ghostSettings.LastManuallyConfigShowHudInfo = value; ghostSettings.ShowInfoEnabler = true; }));
        menu.Add(new TextMenu.OnOff("Show Custom Info".ToDialogText(), ghostSettings.ShowCustomInfo).Change(value => { ghostSettings.ShowCustomInfo = value; ghostSettings.LastManuallyConfigShowCustomInfo = value; ghostSettings.ShowInfoEnabler = true; }));
        menu.Add(new TextMenu.Button("Info Copy Custom Template".ToTASDialogText()).Pressed(() =>
                TextInput.SetClipboardText(string.IsNullOrEmpty(ghostSettings.CustomInfoTemplate) ? "\0" : ghostSettings.CustomInfoTemplate)));
        menu.Add(new TextMenu.Button("Info Set Custom Template".ToTASDialogText()).Pressed(() => {
            ghostSettings.CustomInfoTemplate = TextInput.GetClipboardText() ?? string.Empty;
            GhostModule.Instance.SaveSettings();
        }));

        menu.Add(EnumerableSliderExt<PlayerSpriteMode>.Create("Ghost Sprite Mode".ToDialogText(), CreatePlayerSpriteModeOptions(), ghostSettings.GhostSpriteMode).Change(value => { ghostSettings.GhostSpriteMode = value; GhostReplayer.Clear(); }));



        TextMenu.Item hitboxColor = ColorCustomization.CreateChangeColorItem(() => ghostSettings.HitboxColor, value => ghostSettings.HitboxColor = value, "Hitbox Color".ToDialogText(), menu, inGame, GhostModuleSettings.defaultHitboxColor);
        menu.Add(hitboxColor);
        TextMenu.Item hurtboxColor = ColorCustomization.CreateChangeColorItem(() => ghostSettings.HurtboxColor, value => ghostSettings.HurtboxColor = value, "Hurtbox Color".ToDialogText(), menu, inGame, GhostModuleSettings.defaultHurtboxColor);
        menu.Add(hurtboxColor);
        if (inGame) {
            SubHeaderExt remindText = new("Color Customization Remind".ToDialogText()) {
                TextColor = Color.Gray,
                HeightExtra = 0f
            };
            menu.Add(remindText);
        }
        SubHeaderExt formatText = new("Color Customization Color Format".ToDialogText()) {
            TextColor = Color.Gray,
            HeightExtra = 0f
        };
        menu.Add(formatText);


        menu.Add(new TextMenu.OnOff("Show Recorder Icon".ToDialogText(), ghostSettings.ShowRecorderIcon).Change(value => { ghostSettings.ShowRecorderIcon = value; RecordingIcon.Instance?.Update(); }));
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

    private static IEnumerable<KeyValuePair<bool, string>> CreateRTA_IGTOptions() {
        return new List<KeyValuePair<bool, string>> {
            new(false, "RTA"),
            new(true, "IGT")
        };
    }

    private static IEnumerable<KeyValuePair<TimeFormats, string>> CreateTimeFormatOptions() {
        return new List<KeyValuePair<TimeFormats, string>> {
            new(TimeFormats.SecondAndFrame, "-0.017(-1f)"),
            new(TimeFormats.SecondOnly, "-0.017s"),
            new(TimeFormats.FrameOnly, "-1f")
        };
    }
    private static IEnumerable<KeyValuePair<GhostModuleMode, string>> CreateMainModeOptions() {
        return new List<KeyValuePair<GhostModuleMode, string>> {
            new(GhostModuleMode.Off, "Mode Off".ToDialogText()),
            new(GhostModuleMode.Record, "Mode Record".ToDialogText()),
            new(GhostModuleMode.Play, "Mode Play".ToDialogText()),
            // new(GhostModuleMode.Both, "Mode Both".ToDialogText()),
        };
    }

    private static List<Tuple<PlayerSpriteMode, string, MTexture>> CreatePlayerSpriteModeOptions() {
        return new List<Tuple<PlayerSpriteMode, string, MTexture>>() {
            new(PlayerSpriteMode.Madeline, "Madeline", Madeline),
            new(PlayerSpriteMode.MadelineNoBackpack, "MadelineNoBackpack", NoBackpack),
            new(PlayerSpriteMode.Badeline, "Badeline", Badeline),
            new(PlayerSpriteMode.MadelineAsBadeline, "MadelineAsBadeline", MadelineAsBadeline),
            new(PlayerSpriteMode.Playback, "Playback", Playback)
        };
    }

    [Initialize]

    private static void Initialize() {
        Madeline = GFX.Game["GhostModForTas/madeline"];
        Badeline = GFX.Game["GhostModForTas/badeline"];
        MadelineAsBadeline = GFX.Game["GhostModForTas/madeline_as_badeline"];
        NoBackpack = GFX.Game["GhostModForTas/no_backpack"];
        Playback = GFX.Game["GhostModForTas/playback"];
    }

    public static MTexture Madeline;
    public static MTexture Badeline;
    public static MTexture MadelineAsBadeline;
    public static MTexture NoBackpack;
    public static MTexture Playback;

    private static void AddDescriptionOnEnter(this List<TextMenu.Item> page, TextMenu menu, TextMenu.Item item, string description) {
        EaseInSubHeaderExt descriptionText = new(description, false, menu) {
            TextColor = Color.Gray,
            HeightExtra = 0f
        };
        page.Add(descriptionText);
        item.OnEnter += () => descriptionText.FadeVisible = true;
        item.OnLeave += () => descriptionText.FadeVisible = false;
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


internal class HeaderExt : Item {
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

internal class EnumerableSliderExt<T> : TextMenuExt.EnumerableSlider<T> {

    public Dictionary<string, MTexture> Textures;
    public EnumerableSliderExt(string label, IEnumerable<T> options, T startValue, Dictionary<string, MTexture> textures) : base(label, options, startValue) {
        Textures = textures;
    }

    public override float RightWidth() {
        return 200f;
    }
    public static EnumerableSliderExt<T> Create(string label, List<Tuple<T, string, MTexture>> tuples, T startValue) {
        IEnumerable<T> options = tuples.Select(x => x.Item1);
        Dictionary<string, MTexture> dict = new Dictionary<string, MTexture>();
        tuples.ForEach(x => dict.Add(x.Item2, x.Item3));
        return new EnumerableSliderExt<T>(label, options, startValue, dict);
    }

    public override void Render(Vector2 position, bool highlighted) {
        float alpha = Container.Alpha;
        Color strokeColor = Color.Black * (alpha * alpha * alpha);
        Color color = Disabled ? Color.DarkSlateGray : (highlighted ? Container.HighlightColor : UnselectedColor * alpha);
        ActiveFont.DrawOutline($"{Label}: {Values[Index].Item1}", position, new Vector2(0f, 0.5f), Vector2.One, color, 2f, strokeColor);
        if (Values.Count > 0) {
            float num = RightWidth();
            Textures[Values[Index].Item1].DrawJustified(position + new Vector2(Container.Width - num * 0.5f + (float)lastDir * ValueWiggler.Value * 8f, 0f), new Vector2(0.5f, 0.8f), Color.White, new Vector2(3f, 3f));
            Vector2 vector = Vector2.UnitX * (highlighted ? ((float)Math.Sin(sine * 4f) * 4f) : 0f);
            bool flag = Index > 0;
            Color color2 = (flag ? color : (Color.DarkSlateGray * alpha));
            Vector2 position2 = position + new Vector2(Container.Width - num + 40f + ((lastDir < 0) ? ((0f - ValueWiggler.Value) * 8f) : 0f), 0f) - (flag ? vector : Vector2.Zero);
            ActiveFont.DrawOutline("<", position2, new Vector2(0.5f, 0.5f), Vector2.One, color2, 2f, strokeColor);
            bool flag2 = Index < Values.Count - 1;
            Color color3 = (flag2 ? color : (Color.DarkSlateGray * alpha));
            Vector2 position3 = position + new Vector2(Container.Width - 40f + ((lastDir > 0) ? (ValueWiggler.Value * 8f) : 0f), 0f) + (flag2 ? vector : Vector2.Zero);
            ActiveFont.DrawOutline(">", position3, new Vector2(0.5f, 0.5f), Vector2.One, color3, 2f, strokeColor);
        }
    }
}

internal static class ColorCustomization {
    public static TextMenu.Item CreateChangeColorItem(Func<Color> getter, Action<Color> setter, string name, TextMenu textMenu, bool inGame, Color defaultValue) {
        TextMenu.Item item = new ButtonColorExt(name, getter, inGame).Pressed(inGame ? () => { }
        :
            () => {
                OuiModOptionStringHexColor.DefaultString = ColorToHex(defaultValue);
                Audio.Play("event:/ui/main/savefile_rename_start");
                textMenu.SceneAs<Overworld>().Goto<OuiModOptionStringHexColor>()
                    .Init<OuiModOptions>(ColorToHex(getter()),
                        value => setter(HexToColor(value, getter())), 9);
            });
        return item;
    }
}

public class ButtonColorExt : TextMenu.Button, IItemExt {

    public Func<Color> CubeColorGetter = () => Color.White;
    public Color TextColor { get; set; } = Color.White;

    public string name;
    public Color TextColorDisabled { get; set; } = Color.DarkSlateGray;

    public Color TextColorHighlightDisabled { get; set; } = Color.SlateGray;

    public string Icon { get; set; }

    public float? IconWidth { get; set; }

    public bool IconOutline { get; set; }

    public Vector2 Offset { get; set; }

    public float Alpha { get; set; } = 1f;


    public Vector2 Scale { get; set; } = Vector2.One;

    public bool InGame;

    public override float Height() {
        return base.Height() * Scale.Y;
    }

    public override float LeftWidth() {
        return base.LeftWidth() * Scale.X;
    }

#pragma warning disable CS8625
    public ButtonColorExt(string label, Func<Color> cubecolorGetter, bool inGame = false)
#pragma warning restore CS8625
        : base(label) {
        CubeColorGetter = cubecolorGetter;
        Icon = "";
        name = label;
        InGame = inGame;
    }

    public override void Render(Vector2 position, bool highlighted) {
        Label = name + $": {ColorToHex(CubeColorGetter())}";
        position += Offset;
        float num = Container.Alpha * Alpha;
        Color color = (InGame ? (highlighted ? TextColorHighlightDisabled : TextColorDisabled) : (highlighted ? Container.HighlightColor : TextColor)) * num;
        Color strokeColor = Color.Black * (num * num * num);
        bool flag = Container.InnerContent == TextMenu.InnerContentMode.TwoColumn && !AlwaysCenter;
        Vector2 textPosition = position + (flag ? Vector2.Zero : new Vector2(Container.Width * 0.5f, 0f));
        Vector2 justify = flag ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f);
        float height = ActiveFont.Measure("I").Y / 2f;
        Vector2 cubePosition = textPosition + new Vector2(ActiveFont.Measure(Label).X + 30f, -height / 2f);
        Draw.Rect(cubePosition - new Vector2(4f, 4f), height + 8f, height + 8f, Color.Black);
        Draw.Rect(cubePosition, height, height, CubeColorGetter());
        ActiveFont.DrawOutline(Label, textPosition, justify, Scale, color, 2f, strokeColor);
    }
}
public class OuiModOptionStringHexColor : Oui, OuiModOptions.ISubmenu {

    private static readonly float fscale = 2f;

    public static bool Cancelled;

    public string StartingValue;

    private string _Value;
    public string Value {
        get {
            return _Value;
        }
        set {
            _Value = value;
            OnValueChange?.Invoke(value);
        }
    }

    public int MaxValueLength;
    public int MinValueLength;

    public event Action<string> OnValueChange;

    public event Action<bool> OnExit;

    private string[] letters;
    private int index = 0;
    private int line = 0;
    private float widestLetter;
    private float widestLine;
    private int widestLineCount;
    private bool selectingOptions = true;
    private int optionsIndex;
    private float lineHeight;
    private float lineSpacing;
    private float boxPadding;
    private float optionsScale;
    private string cancel;
    private string reset;
    private string backspace;
    private string accept;
    private float cancelWidth;
    private float resetWidth;
    private float backspaceWidth;
    private float beginWidth;
    private float optionsWidth;
    private float boxWidth;
    private float boxHeight;
    private float pressedTimer;
    private float timer;
    private float ease;

    private Wiggler wiggler;

    private Color unselectColor = Color.LightGray;
    private Color selectColorA = Calc.HexToColor("84FF54");
    private Color selectColorB = Calc.HexToColor("FCFF59");
    private Color disableColor = Color.DarkSlateBlue;

    public static string DefaultString = "#FF000000";

    private Vector2 boxtopleft {
        get {
            return Position + new Vector2((1920f - boxWidth) / 2f, 360f + (680f - boxHeight) / 2f);
        }
    }

    public OuiModOptionStringHexColor()
        : base() {
        wiggler = Wiggler.Create(0.25f, 4f);
        Position = new Vector2(0f, 1080f);
        Visible = false;
    }

    public OuiModOptionStringHexColor Init<T>(string value, Action<string> onValueChange) where T : Oui {
        return Init<T>(value, onValueChange, 12, 1);
    }

    public OuiModOptionStringHexColor Init<T>(string value, Action<string> onValueChange, int maxValueLength) where T : Oui {
        return Init<T>(value, onValueChange, maxValueLength, 1);
    }

    public OuiModOptionStringHexColor Init<T>(string value, Action<string> onValueChange, int maxValueLength, int minValueLength) where T : Oui {
        return Init(value, onValueChange, (confirm) => Overworld.Goto<T>(), maxValueLength, minValueLength);
    }

    public OuiModOptionStringHexColor Init<T>(string value, Action<string> onValueChange, Action<bool> onExit, int maxValueLength, int minValueLength) where T : Oui {
        return Init(value, onValueChange, (confirm) => { Overworld.Goto<T>(); onExit?.Invoke(confirm); }, maxValueLength, minValueLength);
    }

    public OuiModOptionStringHexColor Init(string value, Action<string> onValueChange, Action<bool> exit, int maxValueLength, int minValueLength) {
        _Value = StartingValue = value ?? "";
        OnValueChange = onValueChange;

        MaxValueLength = maxValueLength;
        MinValueLength = minValueLength;

        OnExit += exit;
        Cancelled = false;

        return this;
    }

    public override IEnumerator Enter(Oui from) {
        TextInput.OnInput += OnTextInput;

        Overworld.ShowInputUI = false;

        Engine.Commands.Enabled = false;

        selectingOptions = false;
        optionsIndex = 0;
        index = 0;
        line = 0;

        string letterChars = "01234567\n89ABCDEF";
        letters = letterChars.Split('\n');

        foreach (char c in letterChars) {
            float width = fscale * ActiveFont.Measure(c).X;
            if (width > widestLetter) {
                widestLetter = width;
            }
        }

        widestLineCount = 0;
        foreach (string letter in letters) {
            if (letter.Length > widestLineCount) {
                widestLineCount = letter.Length;
            }
        }

        widestLine = widestLineCount * widestLetter;
#pragma warning disable CS8600
        letterChars = null;
#pragma warning restore CS8600

        lineHeight = fscale * ActiveFont.LineHeight;
        lineSpacing = fscale * ActiveFont.LineHeight * 0.1f;
        boxPadding = widestLetter;
        optionsScale = 0.75f;
        cancel = Dialog.Clean("name_back");
        reset = "Reset to Default".ToDialogText();
        backspace = Dialog.Clean("name_backspace");
        accept = Dialog.Clean("name_accept");
        cancelWidth = ActiveFont.Measure(cancel).X * optionsScale;
        resetWidth = ActiveFont.Measure(reset).X * optionsScale;
        backspaceWidth = ActiveFont.Measure(backspace).X * optionsScale;
        beginWidth = ActiveFont.Measure(accept).X * optionsScale;
        optionsWidth = cancelWidth + resetWidth + backspaceWidth + beginWidth + widestLetter * 3f;
        boxWidth = Math.Max(widestLine, optionsWidth) + boxPadding * 2f;
        boxHeight = (letters.Length + 1f) * lineHeight + letters.Length * lineSpacing + boxPadding * 3f;

        Visible = true;

        Vector2 posFrom = Position;
        Vector2 posTo = Vector2.Zero;
        for (float t = 0f; t < 1f; t += Engine.DeltaTime * 2f) {
            ease = Ease.CubeIn(t);
            Position = posFrom + (posTo - posFrom) * Ease.CubeInOut(t);
            yield return null;
        }
        ease = 1f;
        posFrom = Vector2.Zero;
        posTo = Vector2.Zero;

        yield return 0.2f;

        Focused = true;

        yield return 0.2f;

        wiggler.Start();
    }

    public override IEnumerator Leave(Oui next) {
        TextInput.OnInput -= OnTextInput;

        Overworld.ShowInputUI = true;
        Focused = false;

        Engine.Commands.Enabled = Celeste.PlayMode == Celeste.PlayModes.Debug;

        Vector2 posFrom = Position;
        Vector2 posTo = new Vector2(0f, 1080f);
        for (float t = 0f; t < 1f; t += Engine.DeltaTime * 2f) {
            ease = 1f - Ease.CubeIn(t);
            Position = posFrom + (posTo - posFrom) * Ease.CubeInOut(t);
            yield return null;
        }

        Visible = false;
    }

    public bool UseKeyboardInput {
        get {
            var settings = Core.CoreModule.Instance._Settings as Core.CoreModuleSettings;
            return settings?.UseKeyboardForTextInput ?? false;
        }
    }

    public void OnTextInput(char c) {
        if (!UseKeyboardInput) {
            return;
        }

        if (c == (char)13) {
            // Enter - confirm.
            Finish();

        } else if (c == (char)8) {
            // Backspace - trim.
            Backspace();

        } else if (c == (char)22) {
            // Paste.
            string value = Value + TextInput.GetClipboardText();
            if (value.Length > MaxValueLength)
                value = value.Substring(0, MaxValueLength);
            Value = value;

        } else if (c == (char)127) {
            // Delete - currenly not handled.

        } else if (c == ' ') {
            // Space - append.
            if (Value.Length < MaxValueLength) {
                Audio.Play(SFX.ui_main_rename_entry_space);
                Value += c;
            } else {
                Audio.Play(SFX.ui_main_button_invalid);
            }

        } else if (!char.IsControl(c)) {
            // Any other character - append.
            if (Value.Length < MaxValueLength && ActiveFont.FontSize.Characters.ContainsKey(c)) {
                Audio.Play(SFX.ui_main_rename_entry_char);
                Value += c;
            } else {
                Audio.Play(SFX.ui_main_button_invalid);
            }
        }
    }

    public override void SceneEnd(Scene scene) {
        Overworld.ShowInputUI = true;
        Engine.Commands.Enabled = Celeste.PlayMode == Celeste.PlayModes.Debug;
    }

    public override void Update() {
        bool wasFocused = Focused;

        // Only "focus" if we're not using the keyboard for input
        Focused = wasFocused && !UseKeyboardInput;

        base.Update();

        // TODO: Rewrite or study and document the following code.
        // It stems from OuiFileNaming.

        if (!(Selected && Focused)) {
            goto End;
        }

        if (Input.MenuRight.Pressed && (optionsIndex < 3 || !selectingOptions) && (Value.Length > 0 || !selectingOptions)) {
            if (selectingOptions) {
                optionsIndex = Math.Min(optionsIndex + 1, 3);
            } else {
                do {
                    index = (index + 1) % letters[line].Length;
                } while (letters[line][index] == ' ');
            }
            wiggler.Start();
            Audio.Play(SFX.ui_main_rename_entry_roll);

        } else if (Input.MenuLeft.Pressed && (optionsIndex > 0 || !selectingOptions)) {
            if (selectingOptions) {
                optionsIndex = Math.Max(optionsIndex - 1, 0);
            } else {
                do {
                    index = (index + letters[line].Length - 1) % letters[line].Length;
                } while (letters[line][index] == ' ');
            }
            wiggler.Start();
            Audio.Play(SFX.ui_main_rename_entry_roll);

        } else if (Input.MenuDown.Pressed && !selectingOptions) {
            int lineNext = line + 1;
            bool something = true;
            for (; lineNext < letters.Length; lineNext++) {
                if (index < letters[lineNext].Length && letters[lineNext][index] != ' ') {
                    something = false;
                    break;
                }
            }

            if (something) {
                selectingOptions = true;

            } else {
                line = lineNext;

            }

            if (selectingOptions) {
                float pos = index * widestLetter;
                float offs = boxWidth - boxPadding * 2f;
                if (Value.Length == 0 || pos < cancelWidth + (offs - cancelWidth - beginWidth - backspaceWidth - resetWidth - widestLetter * 3f) / 2f) {
                    optionsIndex = 0;
                } else if (pos < offs - beginWidth - backspaceWidth - widestLetter * 2f) {
                    optionsIndex = 1;
                } else if (pos < offs - beginWidth - widestLetter) {
                    optionsIndex = 2;
                } else {
                    optionsIndex = 3;
                }
            }

            wiggler.Start();
            Audio.Play(SFX.ui_main_rename_entry_roll);

        } else if ((Input.MenuUp.Pressed || selectingOptions && Value.Length <= 0 && optionsIndex > 0) && (line > 0 || selectingOptions)) {
            if (selectingOptions) {
                line = letters.Length;
                selectingOptions = false;
                float offs = boxWidth - boxPadding * 2f;
                if (optionsIndex == 0) {
                    index = (int)(cancelWidth / 2f / widestLetter);
                } else if (optionsIndex == 1) {
                    index = (int)((offs - beginWidth - backspaceWidth - resetWidth / 2f - widestLetter * 2f) / widestLetter);
                } else if (optionsIndex == 2) {
                    index = (int)((offs - beginWidth - backspaceWidth / 2f - widestLetter) / widestLetter);
                } else if (optionsIndex == 3) {
                    index = (int)((offs - beginWidth / 2f) / widestLetter);
                }
            }
            do {
                line--;
            } while (line > 0 && (index >= letters[line].Length || letters[line][index] == ' '));
            while (index >= letters[line].Length || letters[line][index] == ' ') {
                index--;
            }
            wiggler.Start();
            Audio.Play(SFX.ui_main_rename_entry_roll);

        } else if (Input.MenuConfirm.Pressed) {
            if (selectingOptions) {
                if (optionsIndex == 0) {
                    Cancel();
                } else if (optionsIndex == 1) {
                    Reset();
                } else if (optionsIndex == 2) {
                    Backspace();
                } else if (optionsIndex == 3) {
                    Finish();
                }
            } else if (Value.Length < MaxValueLength) {
                Value += letters[line][index].ToString();
                wiggler.Start();
                Audio.Play(SFX.ui_main_rename_entry_char);
            } else {
                Audio.Play(SFX.ui_main_button_invalid);
            }

        } else if (Input.MenuCancel.Pressed) {
            if (Value.Length > 0) {
                Backspace();
            } else {
                Cancel();
            }

        } else if (Input.Pause.Pressed) {
            Finish();
        }

        End:

        if (wasFocused && !Focused) {
            if (Input.ESC) {
                Cancel();
                wasFocused = false;
            }
        }

        Focused = wasFocused;

        pressedTimer -= Engine.DeltaTime;
        timer += Engine.DeltaTime;
        wiggler.Update();
    }

    private void Reset() {
        Value = DefaultString;
        wiggler.Start();
        Audio.Play(SFX.ui_main_rename_entry_char);
    }

    private void Backspace() {
        if (Value.Length > 0) {
            Value = Value.Substring(0, Value.Length - 1);
            Audio.Play(SFX.ui_main_rename_entry_backspace);
        } else {
            Audio.Play(SFX.ui_main_button_invalid);
        }
    }

    private void Finish() {
        if (Value.Length >= MinValueLength) {
            Focused = false;
            OnExit?.Invoke(true);
#pragma warning disable CS8625
            OnExit = null;
#pragma warning restore CS8625
            Audio.Play(SFX.ui_main_rename_entry_accept);
        } else {
            Audio.Play(SFX.ui_main_button_invalid);
        }
    }

    private void Cancel() {
        Cancelled = true;
        Value = StartingValue;
        Focused = false;
        OnExit?.Invoke(false);
#pragma warning disable CS8625
        OnExit = null;
#pragma warning restore CS8625
        Audio.Play(SFX.ui_main_button_back);
    }

    public override void Render() {
        float fscale = OuiModOptionStringHexColor.fscale;
        int prevIndex = index;
        // Only "focus" if we're not using the keyboard for input
        if (UseKeyboardInput)
            index = -1;

        // TODO: Rewrite or study and document the following code.
        // It stems from OuiFileNaming.

        Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * 0.8f * ease);

        Vector2 pos = boxtopleft + new Vector2(boxPadding, boxPadding);
        float posX = boxtopleft.X + boxWidth / 2f - fscale * ActiveFont.Measure("0123").X - widestLetter;
        pos.X = posX;
        int letterIndex = 0;
        foreach (string letter in letters) {
            for (int i = 0; i < letter.Length; i++) {
                bool selected = letterIndex == line && i == index && !selectingOptions;
                Vector2 scale = fscale * Vector2.One * (selected ? 1.2f : 1f);
                Vector2 posLetter = pos + new Vector2(widestLetter, lineHeight) / 2f;
                if (selected) {
                    posLetter += new Vector2(0f, wiggler.Value) * 8f * fscale;
                }
                DrawOptionText(letter[i].ToString(), posLetter, new Vector2(0.5f, 0.5f), scale, selected);
                pos.X += widestLetter;
            }
            pos.X = posX;
            pos.Y += lineHeight + lineSpacing;
            letterIndex++;
        }
        float wiggle = wiggler.Value * 8f;

        pos.Y = boxtopleft.Y + boxHeight - lineHeight - boxPadding;
        pos.X = boxtopleft.X + boxPadding;
        Draw.Rect(pos.X, pos.Y - boxPadding * 0.5f, boxWidth - boxPadding * 2f, 4f, Color.White);
        lineHeight /= fscale;
        DrawOptionText(cancel, pos + new Vector2(0f, lineHeight + (selectingOptions && optionsIndex == 0 ? wiggle : 0f)), new Vector2(0f, 1f), Vector2.One * optionsScale, selectingOptions && optionsIndex == 0);
        pos.X = boxtopleft.X + boxWidth - backspaceWidth - widestLetter - resetWidth - widestLetter - beginWidth - boxPadding;

        DrawOptionText(reset, pos + new Vector2(0f, lineHeight + (selectingOptions && optionsIndex == 1 ? wiggle : 0f)), new Vector2(0f, 1f), Vector2.One * optionsScale, selectingOptions && optionsIndex == 1, Value.Length == 0 || !Focused);
        pos.X += resetWidth + widestLetter;

        DrawOptionText(backspace, pos + new Vector2(0f, lineHeight + (selectingOptions && optionsIndex == 2 ? wiggle : 0f)), new Vector2(0f, 1f), Vector2.One * optionsScale, selectingOptions && optionsIndex == 2, Value.Length <= 0 || !Focused);
        pos.X += backspaceWidth + widestLetter;

        DrawOptionText(accept, pos + new Vector2(0f, lineHeight + (selectingOptions && optionsIndex == 3 ? wiggle : 0f)), new Vector2(0f, 1f), Vector2.One * optionsScale, selectingOptions && optionsIndex == 3, Value.Length < 1 || !Focused);

        ActiveFont.DrawEdgeOutline(Value, Position + new Vector2(960f, 256f), new Vector2(0.5f, 0.5f), Vector2.One * 2f, Color.Gray, 4f, Color.DarkSlateBlue, 2f, Color.Black);
        lineHeight *= fscale;
        index = prevIndex;
    }

    private void DrawOptionText(string text, Vector2 at, Vector2 justify, Vector2 scale, bool selected, bool disabled = false) {
        // Only draw "interactively" if not using the keyboard for input
        if (UseKeyboardInput) {
            selected = false;
            disabled = true;
        }

        Color color = disabled ? disableColor : GetTextColor(selected);
        Color edgeColor = disabled ? Color.Lerp(disableColor, Color.Black, 0.7f) : Color.Gray;
        if (selected && pressedTimer > 0f) {
            ActiveFont.Draw(text, at + Vector2.UnitY, justify, scale, color);
        } else {
            ActiveFont.DrawEdgeOutline(text, at, justify, scale, color, 4f, edgeColor);
        }
    }

    private Color GetTextColor(bool selected) {
        if (selected)
            return Calc.BetweenInterval(timer, 0.1f) ? selectColorA : selectColorB;
        return unselectColor;
    }

}
