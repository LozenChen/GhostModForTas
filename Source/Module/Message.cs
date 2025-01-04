using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;

namespace Celeste.Mod.GhostModForTas.Module;


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
