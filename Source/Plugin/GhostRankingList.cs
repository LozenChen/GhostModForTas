using Celeste.Mod.GhostModForTas.ModInterop;
using Celeste.Mod.GhostModForTas.Module;
using Celeste.Mod.GhostModForTas.Recorder.Data;
using Celeste.Mod.GhostModForTas.Replayer;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.GhostModForTas.Plugin;

public class GhostRankingList : Component {

    private GhostReplayerEntity parent;

    private readonly List<Item> items;

    public static bool ConfigChanged = false;

    public GhostRankingList(GhostReplayerEntity replayer) : base(false, true) {
        parent = replayer;
        items = replayer.Ghosts.Select(x => new Item(new GhostHolder(x))).ToList();
        items.Add(new Item(new GhostHolder(null)));
    }

    public void HandleTransition(string roomName, string target) {
        foreach (Item item in items) {
            item.UpdateTransition(roomName, target);
        }
    }

    public override void Render() {
        if (!ghostSettings.CompareStyleIsModern || !ghostSettings.ShowCompareTime) {
            return;
        }

        if (ConfigChanged) {
            foreach (Item item in items) {
                item.UpdateData();
            }
            ConfigChanged = false;
        }

        float leftWidth = 0f;
        float rightWidth = 0f;
        foreach (Item item in items) {
            leftWidth = System.Math.Max(leftWidth, item.LeftWidth);
            rightWidth = System.Math.Max(rightWidth, item.RightWidth);
        }
        float width = leftWidth + SpacesBetween + rightWidth;
        float itemHeight = ActiveFont.Measure('Y').Y * f_scale;

        Vector2 pos;
        if (Alignment == Alignments.TopRight) {
            pos = TopRight;
            pos.X -= width + Padding * 2;
        } else {
            pos = TopLeft;
            if (Settings.Instance.SpeedrunClock == SpeedrunType.File) {
                pos.Y += 24;
            }
        }

        Vector2 size = new Vector2(width, itemHeight * items.Count);
        Rectangle bgRect = new Rectangle((int)pos.X, (int)pos.Y, (int)size.X + Padding * 2, (int)size.Y + Padding * 2);

        alpha = ghostSettings.ComparerAlpha;
        if (Scene.Paused) {
            alpha *= 0.5f;
        } else if (Scene is Level level && level.Tracker.GetEntity<Player>() is { } player) {
            float pixelScale = 6f;
            Vector2 playerPosition = level.Camera.CameraToScreen(player.TopLeft) * pixelScale;
            Rectangle playerRect = new Rectangle((int)playerPosition.X, (int)playerPosition.Y, (int)(8 * pixelScale), (int)(11 * pixelScale));
            Rectangle mirrorBgRect = bgRect;
            if (SaveData.Instance?.Assists.MirrorMode == true) {
                mirrorBgRect.X = (int)Math.Abs(pos.X - 1920f + size.X + Padding * 2);
            }

            if (ModImports.UpsideDown) {
                mirrorBgRect.Y = (int)Math.Abs(pos.Y - 1080f + size.Y + Padding * 2);
            }

            if (playerRect.Intersects(mirrorBgRect)) {
                alpha *= ModInterop.TasImports.Manager_Running ? 0.5f : 0.2f;
            }
        }

        if (ModInterop.TasImports.ZoomLevel < 0.8f) {
            Draw.Rect(bgRect, Color.Black * alpha); // in this case, we don't need to worry that comparer covers our level
            Draw.HollowRect(bgRect, Color.Silver * alpha);
        } else {
            Draw.Rect(bgRect, Color.Black * (0.3f * alpha));
        }

        pos += Vector2.One * Padding;
        foreach (Item item in items) {
            item.Render(pos, width);
            pos.Y += itemHeight;
        }

    }

    public static Vector2 TopRight = new Vector2(1922f, 100f);

    public static Vector2 TopLeft = new Vector2(0f, 100f);

    public static Alignments Alignment => ghostSettings.ComparerAlignment;

    public static int Padding = 5;

    public static float SpacesBetween = 20f;

    public static float alpha = 1f;

    public static float f_scale = 0.5f;

    private class GhostHolder {

        public Ghost? Ghost;

        public Color Color => Ghost?.Color ?? Color.White;

        public string Name => Ghost?.Name ?? ghostSettings.PlayerName;

        public bool NotSynced => Ghost?.NotSynced ?? false;

        public long LastSessionTime => Ghost?.LastSessionTime ?? GhostCompare.CurrentTime;


        public GhostHolder(Ghost? ghost) {
            Ghost = ghost;
        }
    }
    private class Item {
        public GhostHolder ghost;

        public string name;
        public Color nameColor;

        public long GhostTime;
        public long LastGhostTime;

        public long diffRoomTime;
        public long diffTotalTime;

        public string roomTimeString;
        public Color roomTimeColor;
        public string totalTimeString;
        public Color totalTimeColor;

        public Vector2 Position;
        public bool NotSynced = false;
        public const string NotSyncedString = "N/A";

        public float LeftWidth;

        public float RightWidth;

        public Item(GhostHolder ghost) {
            this.ghost = ghost;
            Init();
        }

        public void Init() {
            nameColor = ghost.Color;
            name = ghost.Name;
            NotSynced = false;
            LastGhostTime = GhostTime = 0;
            UpdateData();
        }
        public void UpdateTransition(string roomName, string target) {
            nameColor = ghost.Color;
            name = ghost.Name;

            NotSynced = ghost.NotSynced || GhostCompare.Complaint != GhostCompare.ComplaintMode.OK; // comparer ghost doesn't exist
            if (NotSynced) {
                UpdateData();
                return;
            }

            if (GhostReplayer.Replayer.ForceSync || ghost.Ghost is null) {
                LastGhostTime = GhostTime;
                GhostTime = ghost.LastSessionTime;
            } else if (ghost.Ghost is Ghost realGhost) {
                bool found = false;
                foreach (GhostData data in realGhost.AllRoomData) {
                    if (data.LevelCount == new LevelCount(roomName, GhostReplayer.Replayer.RevisitCount[roomName]) && data.TargetCount.Level == target) {
                        LastGhostTime = GhostTime;
                        GhostTime = data.GetSessionTime();
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    NotSynced = true; // current ghost doesn't match our route
                    UpdateData();
                    return;
                }
            }

            UpdateData();
        }

        public void UpdateData() {
            diffRoomTime = -(GhostCompare.GhostTime - GhostCompare.LastGhostTime - GhostTime + LastGhostTime);
            diffTotalTime = -(GhostCompare.GhostTime - GhostTime);

            roomTimeString = GhostCompare.FormatTime(diffRoomTime);
            roomTimeColor = AheadBehindColor(diffRoomTime);
            totalTimeString = GhostCompare.FormatTime(diffTotalTime);
            totalTimeColor = AheadBehindColor(diffTotalTime);

            LeftWidth = ActiveFont.Measure(name).X * f_scale;
            if (NotSynced) {
                RightWidth = ActiveFont.Measure(NotSyncedString).X * f_scale;
            } else {
                if (ghostSettings.CompareRoomTime && ghostSettings.CompareTotalTime) {
                    RightWidth = ActiveFont.Measure(roomTimeString + " / " + totalTimeString).X * f_scale;
                } else if (ghostSettings.CompareRoomTime) {
                    RightWidth = ActiveFont.Measure(roomTimeString).X * f_scale;
                } else if (ghostSettings.CompareTotalTime) {
                    RightWidth = ActiveFont.Measure(totalTimeString).X * f_scale;
                } else {
                    RightWidth = 0f;
                }
            }
        }

        public void LeftRender() {
            ActiveFont.DrawOutline(name, Position, new Vector2(0f, 0f), Vector2.One * f_scale, nameColor * alpha, 2f, Color.Black * (alpha * alpha * alpha));
        }

        public void RightRender() {
            if (NotSynced) {
                ActiveFont.Draw(NotSyncedString, Position, new Vector2(1f, 0f), Vector2.One * f_scale, Color.Red * alpha);
                return;
            }
            if (ghostSettings.CompareRoomTime && ghostSettings.CompareTotalTime) {
                ActiveFont.Draw(totalTimeString, Position, new Vector2(1f, 0f), Vector2.One * f_scale, totalTimeColor * alpha);
                Position.X -= ActiveFont.Measure(totalTimeString).X * f_scale;
                ActiveFont.Draw(" / ", Position, new Vector2(1f, 0f), Vector2.One * f_scale, Color.White * alpha);
                Position.X -= ActiveFont.Measure(" / ").X * f_scale;
                ActiveFont.Draw(roomTimeString, Position, new Vector2(1f, 0f), Vector2.One * f_scale, roomTimeColor * alpha);
            } else if (ghostSettings.CompareRoomTime) {
                ActiveFont.Draw(roomTimeString, Position, new Vector2(1f, 0f), Vector2.One * f_scale, roomTimeColor * alpha);
            } else if (ghostSettings.CompareTotalTime) {
                ActiveFont.Draw(totalTimeString, Position, new Vector2(1f, 0f), Vector2.One * f_scale, totalTimeColor * alpha);
            }
        }

        public void Render(Vector2 position, float width) {
            Position = position;
            LeftRender();
            Position.X += width;
            RightRender();
        }
    }

    public static Color AheadBehindColor(float diffTime) => GhostCompare.AheadBehindColor(diffTime);
}