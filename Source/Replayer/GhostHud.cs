using Celeste.Mod.GhostModForTas.Module;
using Celeste.Mod.GhostModForTas.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Linq;
using System.Text;
using TAS.EverestInterop;
using TAS.EverestInterop.InfoHUD;
using _Celeste = Celeste;

namespace Celeste.Mod.GhostModForTas.Replayer;

// copy from TAS.EverestInterop.InfoHUD.InfoHud
internal static class GhostHud {

    public static Rectangle bgRect = default;

    [Load]
    private static void Load() {
        On.Celeste.Level.Render += LevelOnRender;
    }

    [Unload]
    private static void Unload() {
        On.Celeste.Level.Render -= LevelOnRender;
    }

    [Initialize]
    private static void Initialize() {
        typeof(InfoMouse).GetMethodInfo("MoveInfoHud").IlHook(il => {
            ILCursor cursor = new(il);
            cursor.Goto(-1);
            if (cursor.TryGotoPrev(ins => ins.OpCode == OpCodes.Brfalse_S)) {
                cursor.Index++;
                Instruction target = cursor.Next;
                cursor.EmitDelegate(DragAndMoveHud);
                cursor.Emit(OpCodes.Brfalse, target);
                cursor.Emit(OpCodes.Ret);
            }

        });
    }

    private static void LevelOnRender(On.Celeste.Level.orig_Render orig, Level self) {
        orig(self);
        DrawInfo(self);
    }

    private static bool DragAndMoveHud() {
        if (infoIsDrawn && bgRect.Contains((int)MouseButtons.LastPosition.X, (int)MouseButtons.LastPosition.Y)) {
            ghostSettings.InfoPosition += MouseButtons.Position - MouseButtons.LastPosition;
            GhostModule.Instance.SaveSettings();
            return true;
        }
        return false;
    }

    private static bool infoIsDrawn = false;
    private static void DrawInfo(Scene scene) {
        // we also stop to draw info if the orig info hud doesn't draw
        infoIsDrawn = false;
        if (!ghostSettings.ShowInfo || !TasSettings.Enabled || !TasSettings.InfoHud || (GhostModule.ModuleSettings.Mode & GhostModuleMode.Play) != GhostModuleMode.Play) {
            return;
        }

        if (GhostReplayer.Replayer?.ComparerGhost is not Ghost ghost) {
            return;
        }

        StringBuilder stringBuilder = new();
        if (ghostSettings.ShowHudInfo && ghost.HudInfo.IsNotNullOrEmpty()) {
            stringBuilder.Append(ghost.HudInfo).AppendLine();
        }
        if (ghostSettings.ShowCustomInfo) {
            stringBuilder.Append(ghost.CustomInfo);
        }

        string text = stringBuilder.ToString().Trim();

        if (string.IsNullOrEmpty(text)) {
            return;
        }

        string title = ghost.Name + " " + GhostCompare.FormatTime(ghost.AllRoomData.LastOrDefault().GetSessionTime(), true) + (ghost.IsCompleted > 0 ? "" : "(Not Completed)");
        Color titleColor = ghost.IsCompleted > 0 ? Color.Yellow : Color.SlateGray;

        int viewWidth = Engine.ViewWidth;
        int viewHeight = Engine.ViewHeight;

        float titleScale = 1.2f;
        float pixelScale = Engine.ViewWidth / 320f;
        float margin = 2 * pixelScale;
        float padding = 2 * pixelScale;
        float fontSize = 0.15f * pixelScale * TasSettings.InfoTextSize / 10f;
        float alpha = TasSettings.InfoOpacity / 10f;
        float infoAlpha = 1f;

        Vector2 titleSize = JetBrainsMonoFont.Measure(title) * titleScale * fontSize;
        Vector2 contentSize = JetBrainsMonoFont.Measure(text) * fontSize;

        Vector2 totalSize = new Vector2(Math.Max(titleSize.X, contentSize.X), titleSize.Y + contentSize.Y);

        float maxX = viewWidth - totalSize.X - margin - padding * 2;
        float maxY = viewHeight - totalSize.Y - margin - padding * 2;
        if (maxY > 0f) {
            ghostSettings.InfoPosition = ghostSettings.InfoPosition.Clamp(margin, margin, maxX, maxY);
        }

        float x = ghostSettings.InfoPosition.X;
        float y = ghostSettings.InfoPosition.Y;

        bgRect = new((int)x, (int)y, (int)(totalSize.X + padding * 2), (int)(totalSize.Y + padding * 2));

        if (TasSettings.InfoMaskedOpacity < 10 && !Hotkeys.InfoHud.Check && (scene.Paused && !_Celeste.Input.MenuJournal.Check || scene is Level level && CollidePlayer(level, bgRect))) {
            alpha *= TasSettings.InfoMaskedOpacity / 10f;
            infoAlpha = alpha;
        }

        Draw.SpriteBatch.Begin();

        Draw.Rect(bgRect, Color.Black * alpha);

        Vector2 textPosition = new(x + padding, y + padding);
        Vector2 scale = new(fontSize);

        JetBrainsMonoFont.Draw(title, textPosition, Vector2.Zero, titleScale * scale, titleColor * infoAlpha);
        textPosition += Vector2.UnitY * titleSize.Y;
        JetBrainsMonoFont.Draw(text, textPosition, Vector2.Zero, scale, Color.White * infoAlpha);

        Draw.SpriteBatch.End();
        infoIsDrawn = true;
    }

    private static bool CollidePlayer(Level level, Rectangle bgRect) {
        if (level.GetPlayer() is not { } player) {
            return false;
        }

        Vector2 playerTopLeft = level.WorldToScreen(player.TopLeft) / Engine.Width * Engine.ViewWidth;
        Vector2 playerBottomRight = level.WorldToScreen(player.BottomRight) / Engine.Width * Engine.ViewWidth;
        Rectangle playerRect = new(
            (int)Math.Min(playerTopLeft.X, playerBottomRight.X),
            (int)Math.Min(playerTopLeft.Y, playerBottomRight.Y),
            (int)Math.Abs(playerTopLeft.X - playerBottomRight.X),
            (int)Math.Abs(playerTopLeft.Y - playerBottomRight.Y)
        );

        return playerRect.Intersects(bgRect);
    }
}