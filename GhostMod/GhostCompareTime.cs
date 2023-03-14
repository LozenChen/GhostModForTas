using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Ghost;

internal static class GhostCompareTime {
    public static long GhostTime;
    public static long LastGhostTime;
    public static long CurrentTime;
    public static long LastCurrentTime;

    public static void Load() {
        On.Celeste.Level.Render += LevelOnRender;
        On.Celeste.Level.NextLevel += LevelOnNextLevel;
        On.Celeste.Level.RegisterAreaComplete += LevelOnRegisterAreaComplete;
        On.Celeste.LevelLoader.ctor += LevelLoaderOnCtor;
    }

    public static void Unload() {
        On.Celeste.Level.Render -= LevelOnRender;
        On.Celeste.Level.NextLevel -= LevelOnNextLevel;
        On.Celeste.Level.RegisterAreaComplete -= LevelOnRegisterAreaComplete;
        On.Celeste.LevelLoader.ctor -= LevelLoaderOnCtor;
    }

    private static void LevelOnNextLevel(On.Celeste.Level.orig_NextLevel orig, Level self, Vector2 at, Vector2 dir) {
        orig(self, at, dir);
        if (GhostModule.Instance.GhostManager?.Ghosts.FirstOrDefault()?.Data.Frames.LastOrDefault().Data.Time is { } time) {
            LastGhostTime = GhostTime;
            GhostTime = time;
            LastCurrentTime = CurrentTime;
            CurrentTime = self.Session.Time;
        }
    }

    private static void LevelOnRegisterAreaComplete(On.Celeste.Level.orig_RegisterAreaComplete orig, Level self) {
        orig(self);

        if (GhostModule.Instance.GhostManager?.Ghosts.FirstOrDefault()?.Data.Frames.LastOrDefault().Data.Time is long time) {
            LastGhostTime = GhostTime;
            GhostTime = time;
            LastCurrentTime = CurrentTime;
            CurrentTime = self.Session.Time;
        }
    }

    private static void LevelOnRender(On.Celeste.Level.orig_Render orig, Level self) {
        orig(self);

        if (GhostModule.ModuleSettings.Mode == GhostModuleMode.Play && GhostModule.ModuleSettings.ShowCompareTime) {
            int viewWidth = Engine.ViewWidth;
            int viewHeight = Engine.ViewHeight;

            float pixelScale = viewWidth / 320f;
            float margin = 2 * pixelScale;
            float padding = 2 * pixelScale;
            float fontSize = 0.3f * pixelScale;
            float alpha = 1f;

            if (GhostTime == 0) {
                return;
            }

            long diffRoomTime = CurrentTime - GhostTime - LastCurrentTime + LastGhostTime;
            long diffTotalTime = CurrentTime - GhostTime;
            string diffRoomTimeStr = (diffRoomTime > 0 ? "+" : string.Empty) + (diffRoomTime / 10000000D).ToString("0.000");
            string diffTotalTimeStr = (diffTotalTime > 0 ? "+" : string.Empty) + (diffTotalTime / 10000000D).ToString("0.000");
            string timeStr = $"last room: {diffRoomTimeStr}\ntotal    : {diffTotalTimeStr}";

            if (string.IsNullOrEmpty(timeStr)) {
                return;
            }

            Vector2 size = Draw.DefaultFont.MeasureString(timeStr) * fontSize;

            float x;
            float y;

            x = margin;
            y = margin;

            if (Settings.Instance.SpeedrunClock == SpeedrunType.Chapter) {
                y += 16 * pixelScale;
            } else if (Settings.Instance.SpeedrunClock == SpeedrunType.File) {
                y += 20 * pixelScale;
            }

            Rectangle bgRect = new Rectangle((int)x, (int)y, (int)(size.X + padding * 2), (int)(size.Y + padding * 2));

            if (self.Entities.FindFirst<Player>() is Player player) {
                Vector2 playerPosition = self.Camera.CameraToScreen(player.TopLeft) * pixelScale;
                Rectangle playerRect = new Rectangle((int)playerPosition.X, (int)playerPosition.Y, (int)(8 * pixelScale), (int)(11 * pixelScale));
                Rectangle mirrorBgRect = bgRect;
                if (SaveData.Instance?.Assists.MirrorMode == true) {
                    mirrorBgRect.X = (int)Math.Abs(x - viewWidth + size.X + padding * 2);
                }

                if (self.Paused || playerRect.Intersects(mirrorBgRect)) {
                    alpha = 0.5f;
                }
            }

            Draw.SpriteBatch.Begin();

            Draw.Rect(bgRect, Color.Black * 0.8f * alpha);

            Vector2 textPosition = new Vector2(x + padding, y + padding);
            Vector2 scale = new Vector2(fontSize);

            Draw.Text(Draw.DefaultFont, timeStr, textPosition, Color.White * alpha, Vector2.Zero, scale, 0f);

            Draw.SpriteBatch.End();
        }
    }

    private static void LevelLoaderOnCtor(On.Celeste.LevelLoader.orig_ctor orig, LevelLoader self, Session session, Vector2? startPosition) {
        orig(self, session, startPosition);

        GhostTime = 0;
        LastGhostTime = 0;
        CurrentTime = 0;
        LastCurrentTime = 0;
    }
}