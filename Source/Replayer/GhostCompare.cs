using Celeste.Mod.GhostModForTas.ModInterop;
using Celeste.Mod.GhostModForTas.Module;
using Celeste.Mod.GhostModForTas.Plugin;
using Celeste.Mod.GhostModForTas.Recorder;
using Celeste.Mod.GhostModForTas.Recorder.Data;
using Celeste.Mod.GhostModForTas.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.GhostModForTas.Replayer;

internal static class GhostCompare {
    public static long GhostTime;
    public static long LastGhostTime;
    public static long CurrentTime;
    public static long LastCurrentTime;

    public static readonly Color AheadColor = Calc.HexToColor("6ded87");
    public static readonly Color BehindColor = Calc.HexToColor("ff8c8c");

    public static readonly string StringLastRoom = "last room: ";
    public static readonly string StringTotal = "total    : ";

    public static readonly string CorruptedData = "N/A";

    [Load]
    public static void Load() {
        On.Celeste.Level.Render += LevelOnRender;
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Level.Render -= LevelOnRender;
    }

    public static void UpdateRoomTime(Level level, long time, LevelCount lc) {
        LastGhostTime = GhostTime;
        GhostTime = time;
        LastCurrentTime = CurrentTime;
        CurrentTime = ghostSettings.IsIGT ? level.Session.Time : GhostRecorder.RTASessionTime;
        Complaint = ComplaintMode.OK;
        ImprovementTracker.Add(diffRoomTime: CurrentTime - GhostTime - LastCurrentTime + LastGhostTime, lc);
    }

    public static void ResetCompareTime() {
        GhostTime = 0;
        LastGhostTime = 0;
        CurrentTime = 0;
        LastCurrentTime = 0;
        Complaint = ComplaintMode.OK;
        ImprovementTracker.SetTrackLost("Ghost Replayer is reset");
    }

    private static void LevelOnRender(On.Celeste.Level.orig_Render orig, Level self) {
        orig(self);

        if (ghostSettings.CompareStyleIsModern) {
            return;
        }
        if (GhostTime == 0 && Complaint == ComplaintMode.OK) {
            return;
        }
        if ((GhostModule.ModuleSettings.Mode & GhostModuleMode.Play) == GhostModuleMode.Play && GhostModule.ModuleSettings.ShowCompareTime) {
            bool lastRoomDiffCorrupted = Complaint == ComplaintMode.GhostChange;
            int viewWidth = Engine.ViewWidth;

            float pixelScale = viewWidth / 320f;
            float margin = 2 * pixelScale;
            float padding = 2 * pixelScale;
            float fontSize = 0.3f * pixelScale;
            float alpha = ghostSettings.ComparerAlpha;

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
                if (lastRoomDiffCorrupted) {
                    timeStr2 = CorruptedData;
                    color2 = BehindColor;
                }
                if (GhostModule.ModuleSettings.CompareTotalTime) {
                    timeStr3 = StringTotal;
                    timeStr4 = $"{FormatTime(diffTotalTime)}";
                    color4 = AheadBehindColor(diffTotalTime);
                } else {
                    timeStr3 = timeStr4 = "";
                }
            } else {
                timeStr1 = StringTotal;
                timeStr2 = $"{FormatTime(diffTotalTime)}";
                timeStr3 = timeStr4 = "";
                color2 = AheadBehindColor(diffTotalTime);
            }
            if (Complaint == ComplaintMode.NoGhost) {
                timeStr2 = timeStr4 = "";
            }
            timeStr = timeStr1 + timeStr2
                + ((timeStr3 == "") ? "" : "\n" + timeStr3 + timeStr4)
                + ((Complaint == ComplaintMode.OK) ? "" : "\n" + ComplaintText);

            Vector2 size = Draw.DefaultFont.MeasureString(timeStr) * fontSize;

            float x;
            float y;

            if (ghostSettings.ComparerAlignment == Alignments.TopLeft) {
                x = margin;
            } else {
                x = 320f * pixelScale - size.X - padding * 2 + 1f;
            }

            y = margin;

            if (ghostSettings.ComparerAlignment == Alignments.TopLeft) {
                if (Settings.Instance.SpeedrunClock == SpeedrunType.Chapter) {
                    y += 16 * pixelScale;
                } else if (Settings.Instance.SpeedrunClock == SpeedrunType.File) {
                    y += 20 * pixelScale;
                }
            } else {
                y += 2 * pixelScale;
            }

            Rectangle bgRect = new Rectangle((int)x, (int)y, (int)(size.X + padding * 2), (int)(size.Y + padding * 2));

            if (self.Tracker.GetEntity<Player>() is { } player) {
                Vector2 playerPosition = self.Camera.CameraToScreen(player.TopLeft) * pixelScale;
                Rectangle playerRect = new Rectangle((int)playerPosition.X, (int)playerPosition.Y, (int)(8 * pixelScale), (int)(11 * pixelScale));
                Rectangle mirrorBgRect = bgRect;
                if (SaveData.Instance?.Assists.MirrorMode == true) {
                    mirrorBgRect.X = (int)Math.Abs(x - viewWidth + size.X + padding * 2);
                }

                if (ModImports.UpsideDown) {
                    mirrorBgRect.Y = (int)Math.Abs(y - Engine.ViewHeight + size.Y + padding * 2);
                }

                if (self.Paused || playerRect.Intersects(mirrorBgRect)) {
                    alpha *= 0.5f;
                }
            }

            Draw.SpriteBatch.Begin();

            Draw.Rect(bgRect, Color.Black * (0.8f * alpha));

            Vector2 textPosition = new Vector2(x + padding, y + padding);
            Vector2 scale = new Vector2(fontSize);
            Vector2 offset = Draw.DefaultFont.MeasureString(timeStr1) * fontSize;

            Draw.Text(Draw.DefaultFont, timeStr1, textPosition, Color.White * alpha, Vector2.Zero, scale, 0f);
            Draw.Text(Draw.DefaultFont, timeStr2, textPosition + Vector2.UnitX * offset.X, color2 * alpha, Vector2.Zero, scale, 0f);
            if (timeStr3 != "") {
                textPosition += Vector2.UnitY * offset.Y;
                Draw.Text(Draw.DefaultFont, timeStr3, textPosition, Color.White * alpha, Vector2.Zero, scale, 0f);
                Draw.Text(Draw.DefaultFont, timeStr4, textPosition + Vector2.UnitX * offset.X, color4 * alpha, Vector2.Zero, scale, 0f);
            }
            if (ComplaintText.IsNotNullOrEmpty()) {
                textPosition += Vector2.UnitY * offset.Y;
                Draw.Text(Draw.DefaultFont, ComplaintText, textPosition, Color.Red * alpha, Vector2.Zero, scale, 0f);
            }

            Draw.SpriteBatch.End();
        }
    }

    internal static string FormatTime(long time, bool ignorePlus = false, TimeFormats? format = null) {
        string sign = time > 0 ? (ignorePlus ? "" : "+") : time < 0 ? "-" : " ";
        string sign2 = sign == " " ? "" : sign;
        time = Math.Abs(time);
        TimeSpan timeSpan = TimeSpan.FromTicks(time);
        return (format ?? ghostSettings.TimeFormat) switch {
            TimeFormats.SecondAndFrame => $"{sign}{timeSpan.VeryShortGameplayFormat()}({sign2}{time / TimeSpanFix.SecondsToTicks(Engine.RawDeltaTime)}f)",
            TimeFormats.SecondOnly => $"{sign}{timeSpan.VeryShortGameplayFormat()}{(timeSpan.TotalMinutes >= 1.0 ? "" : "s")}",
            TimeFormats.FrameOnly => $"{sign}{time / TimeSpanFix.SecondsToTicks(Engine.RawDeltaTime)}f",
            _ => "",
        };
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


    public enum ComplaintMode { GhostChange, NoGhost, OK }

    private static ComplaintMode backing_Complaint = ComplaintMode.OK;

    public static ComplaintMode Complaint {
        get => backing_Complaint;
        private set {
            backing_Complaint = value;
        }
    }

    public static void Complain(ComplaintMode mode) {
        Complaint = mode;
        if (mode != ComplaintMode.OK) {
            ImprovementTracker.SetTrackLost(Complaint switch {
                ComplaintMode.GhostChange => "Comparer Ghost changes",
                ComplaintMode.NoGhost => "No Ghost in some room",
                _ => ""
            });
        }
    }
    public static string ComplaintText => Complaint switch {
        ComplaintMode.GhostChange => "Comparer Ghost changes",
        ComplaintMode.NoGhost => "No Ghost in this room",
        _ => ""
    };
}