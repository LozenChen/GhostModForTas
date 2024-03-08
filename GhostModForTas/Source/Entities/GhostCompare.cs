using Celeste.Mod.GhostModForTas.Module;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.GhostModForTas.Entities;

internal static class GhostCompare {
    public static long GhostTime;
    public static long LastGhostTime;
    public static long CurrentTime;
    public static long LastCurrentTime;

    public static readonly Color AheadColor = Calc.HexToColor("6ded87");
    public static readonly Color BehindColor = Calc.HexToColor("ff8c8c");

    public static readonly string StringLastRoom = "last room: ";
    public static readonly string StringTotal = "total    : ";

    [Load]
    public static void Load() {
        On.Celeste.Level.Render += LevelOnRender;
        On.Celeste.LevelLoader.ctor += LevelLoaderOnCtor;
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Level.Render -= LevelOnRender;
        On.Celeste.LevelLoader.ctor -= LevelLoaderOnCtor;
    }

    public static void UpdateRoomTime(Level level, long time) {
        LastGhostTime = GhostTime;
        GhostTime = time;
        LastCurrentTime = CurrentTime;
        CurrentTime = level.Session.Time;
    }

    private static void LevelOnRender(On.Celeste.Level.orig_Render orig, Level self) {
        orig(self);

        if (GhostTime == 0) {
            return;
        }
        if ((GhostModule.ModuleSettings.Mode & GhostModuleMode.Play) == GhostModuleMode.Play && GhostModule.ModuleSettings.ShowCompareTime) {
            int viewWidth = Engine.ViewWidth;

            float pixelScale = viewWidth / 320f;
            float margin = 2 * pixelScale;
            float padding = 2 * pixelScale;
            float fontSize = 0.3f * pixelScale;
            float alpha = 1f;

            long diffRoomTime = CurrentTime - GhostTime - LastCurrentTime + LastGhostTime;
            long diffTotalTime = CurrentTime - GhostTime;
            string timeStr;
            string timeStr1;
            string timeStr2;
            string timeStr3;
            string timeStr4;
            Color color2;
            Color color4 = Color.White;
            if (GhostModule.ModuleSettings.CompareRoomTime) {
                timeStr1 = StringLastRoom;
                timeStr2 = $"{FormatTime(diffRoomTime)}";
                color2 = AheadBehindColor(diffRoomTime);
                if (GhostModule.ModuleSettings.CompareTotalTime) {
                    timeStr3 = StringTotal;
                    timeStr4 = $"{FormatTime(diffTotalTime)}";
                    color4 = AheadBehindColor(diffTotalTime);
                    timeStr = timeStr1 + timeStr2 + "\n" + timeStr3 + timeStr4;
                } else {
                    timeStr3 = timeStr4 = "";
                    timeStr = timeStr1 + timeStr2;
                }
            } else {
                timeStr1 = StringTotal;
                timeStr2 = $"{FormatTime(diffTotalTime)}";
                timeStr3 = timeStr4 = "";
                color2 = AheadBehindColor(diffTotalTime);
                timeStr = timeStr1 + timeStr2;
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

            if (self.Entities.FindFirst<Player>() is { } player) {
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
            Vector2 offset = Draw.DefaultFont.MeasureString(timeStr1) * fontSize;

            Draw.Text(Draw.DefaultFont, timeStr1, textPosition, Color.White * alpha, Vector2.Zero, scale, 0f);
            Draw.Text(Draw.DefaultFont, timeStr2, textPosition + Vector2.UnitX * offset.X, color2 * alpha, Vector2.Zero, scale, 0f);
            Draw.Text(Draw.DefaultFont, timeStr3, textPosition + Vector2.UnitY * offset.Y, Color.White * alpha, Vector2.Zero, scale, 0f);
            Draw.Text(Draw.DefaultFont, timeStr4, textPosition + offset, color4 * alpha, Vector2.Zero, scale, 0f);

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

    private static string FormatTime(long time) {
        string sign = time > 0 ? "+" : time < 0 ? "-" : " ";
        TimeSpan timeSpan = TimeSpan.FromTicks(time);
        return $"{sign}{timeSpan.VeryShortGameplayFormat()}({time / TimeSpan.FromSeconds(Engine.RawDeltaTime).Ticks}f)";
    }

    public static string VeryShortGameplayFormat(this TimeSpan time) {
        if (time.TotalHours >= 1.0) {
            return (int)time.TotalHours + ":" + time.ToString("mm\\:ss\\.fff");
        }
        if (time.TotalMinutes >= 1.0) {
            return time.ToString("m\\:ss\\.fff");
        }
        return time.ToString("s\\.fff");
    }

    public static Color AheadBehindColor(float diffTime) {
        if (diffTime < 0) {
            return AheadColor;
        } else if (diffTime > 0) {
            return BehindColor;
        } else {
            return Color.White;
        }
    }
}