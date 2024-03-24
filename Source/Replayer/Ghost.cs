using Celeste.Mod.GhostModForTas.Recorder.Data;
using Celeste.Mod.GhostModForTas.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.GhostModForTas.Replayer;

public class Ghost : Actor {
    public bool Done;
    public bool ForceSync;
    public bool NotSynced;
    public long LastSessionTime;

    public GhostData Data;
    public List<GhostData> AllRoomData;
    private int currentRoomByOrder;
    public int CurrentRoomByOrder {
        get => currentRoomByOrder;
        set {
            currentRoomByOrder = value;
            if (currentRoomByOrder < AllRoomData.Count) {
                Data = AllRoomData[currentRoomByOrder];
            }
        }
    }
    public int FrameIndex;
    public GhostChunkData Frame => Data[FrameIndex].ChunkData;

    public string HudInfo {
        get {
            if (NotSynced) {
                return Frame.HudInfo;
            }
            if (FrameIndex < Data.Frames.Count - 1) {
                return Data[FrameIndex + 1].ChunkData.HudInfo;
            } else if (CurrentRoomByOrder < AllRoomData.Count - 1) {
                return AllRoomData[CurrentRoomByOrder + 1][0].ChunkData.HudInfo;
            } else {
                return "EndOfGhost";
            }
        }
    }
    public string CustomInfo => Frame.CustomInfo;

    public string Name => Data.Name;
    public bool InFreezeFrame => Frame.IsFreezeFrame;

    public int IsCompleted = 0;

    private static Vector2 debugOffset = new Vector2(2f, -2f);
    public PlayerSprite Sprite;
    public PlayerHair Hair;
    public PlayerSpriteMode SpriteMode;
    public Color Color = Color.White;

    public float w1;
    public float h1;
    public float x1;
    public float y1;
    public float w2;
    public float h2;
    public float x2;
    public float y2;


    public Ghost(List<GhostData> allData)
        : base(Vector2.Zero) {
        Tag = Tags.Global;
        Active = false;
        AllRoomData = allData;
        CurrentRoomByOrder = 0;
        Done = false;
        Depth = 1;
        SpriteMode = ghostSettings.GhostSpriteMode;
        Sprite = new PlayerSprite(SpriteMode);
        Add(Hair = new PlayerHair(Sprite));
        Add(Sprite); // add it later so it renders above hair
        Hair.Color = OrigHairColor = SpriteMode switch {
            PlayerSpriteMode.Madeline or PlayerSpriteMode.MadelineNoBackpack or PlayerSpriteMode.Playback => Player.NormalHairColor,
            PlayerSpriteMode.Badeline or PlayerSpriteMode.MadelineAsBadeline => Player.NormalBadelineHairColor
        };
        FrameIndex = -1;
        IsCompleted = allData.LastOrDefault().IsCompleted ? 1 : 0;
        ModImports.AddGravityComponent(this);
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        Hair.Facing = Frame.Facing;
        Hair.Start();
        UpdateHair();
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
    }

    public void UpdateByReplayer() {
        if (Done || NotSynced) {
            return;
        }
        FrameIndex++;

        if (FrameIndex < 0) { // how
            return;
        }
        if (FrameIndex >= Data.Frames.Count) {
            GotoNextRoom();
            if (Done) {
                return;
            }
        }
        Visible &= Frame.HasPlayer;
        ModImports.SetActorGravity(this, Frame.IsInverted);
        UpdateSprite();
        UpdateHair();
        UpdateHitbox();
        base.Update();
    }

    public void GotoNextRoom() {
        LastSessionTime = Data.GetSessionTime();
        CurrentRoomByOrder++;
        if (CurrentRoomByOrder < AllRoomData.Count) {
            FrameIndex = 0;
            NotSynced = ForceSync;
        } else {
            Done = true;
        }
    }

    public void Sync(LevelCount lc) {
        NotSynced = true;
        if (lc == LevelCount.Exit) {
            if (IsCompleted == 0) {
                return;
            }
            Done = true;
            CurrentRoomByOrder = AllRoomData.Count - 1;
            LastSessionTime = Data.GetSessionTime();
            FrameIndex = Data.Frames.Count - 1;
            NotSynced = false;
            return;
        }
        if (Done) {
            return;
        }
        int orig = CurrentRoomByOrder;
        for (int i = orig; i < AllRoomData.Count; i++) {
            if (AllRoomData[i].LevelCount == lc) {
                LastSessionTime = i > 0 ? AllRoomData[i - 1].GetSessionTime() : 0;
                CurrentRoomByOrder = i;
                FrameIndex = -1; // so it becomes 0 after update
                NotSynced = false;
                return;
            }
        }
        for (int i = 0; i < orig - 1; i++) {
            if (AllRoomData[i].LevelCount == lc) {
                LastSessionTime = i > 0 ? AllRoomData[i - 1].GetSessionTime() : 0;
                CurrentRoomByOrder = i;
                FrameIndex = -1;
                NotSynced = false;
                return;
            }
        }
    }

    public override void Update() {
        // do nothing
    }

    public Color OrigHairColor;
    public void UpdateHair() {
        if (!Frame.HasPlayer) {
            return;
        }

        Hair.Facing = Frame.Facing;
        Hair.SimulateMotion = Frame.HairSimulateMotion;
        /*
        Hair.Color = new Color(
            (OrigHairColor.R * Color.R) / 255,
            (OrigHairColor.G * Color.G) / 255,
            (OrigHairColor.B * Color.B) / 255,
            (OrigHairColor.A * Color.A) / 255
        );
 */
        Hair.Color = new Color(
            (Frame.HairColor.R * Color.R) / 255,
            (Frame.HairColor.G * Color.G) / 255,
            (Frame.HairColor.B * Color.B) / 255,
            (Frame.HairColor.A * Color.A) / 255
        );

    }

    public void UpdateSprite() {
        if (!Frame.HasPlayer) {
            return;
        }

        // Position = Frame.Position + debugOffset;
        Position = Frame.Position;

        Sprite.Rotation = Frame.Rotation;
        Sprite.Scale = Frame.Scale;
        Sprite.Scale.X *= (float)Frame.Facing;
        if (SpriteMode == PlayerSpriteMode.Playback) {
            Sprite.Color = Hair.Color;
        } else {
            Sprite.Color = new Color(
                (Frame.Color.R * Color.R) / 255,
                (Frame.Color.G * Color.G) / 255,
                (Frame.Color.B * Color.B) / 255,
                (Frame.Color.A * Color.A) / 255
            );
        }


        Sprite.HairCount = Frame.HairCount;

        try {
            if (Sprite.CurrentAnimationID != Frame.CurrentAnimationID) {
                Sprite.Play(Frame.CurrentAnimationID);
            }

            Sprite.SetAnimationFrame(Frame.CurrentAnimationFrame);
        } catch {
            // Play likes to fail randomly as the ID doesn't exist in an underlying dict.
            // Let's ignore this for now.
        }
        if (Frame.UpdateHair) {
            Hair.AfterUpdate();
        }
    }

    public void UpdateHitbox() {
        if (!Frame.HasPlayer) {
            return;
        }
        w1 = Frame.HitboxWidth;
        h1 = Frame.HitboxHeight;
        x1 = Frame.HitboxLeft;
        y1 = Frame.HitboxTop;
        w2 = Frame.HurtboxWidth;
        h2 = Frame.HurtboxHeight;
        x2 = Frame.HurtboxLeft;
        y2 = Frame.HurtboxTop;
    }

    public override void Render() {
        Sprite.Scale.Y *= Frame.IsInverted ? -1 : 1;
        base.Render();
        Sprite.Scale.Y *= Frame.IsInverted ? -1 : 1;
    }

    public override void DebugRender(Camera camera) {
        base.DebugRender(camera);
        if (Visible && ghostSettings.ShowGhostHitbox) {
            // we dont need to handle GravityHelper Position/Hitbox here, coz what we recorded is already inverted
            DrawHitbox(x1, y1, w1, h1, HitboxColor);
            DrawHitbox(x2, y2, w2, h2, HurtboxColor);
        }
    }


    public Color HitboxColor = ghostSettings.HitboxColor;

    public Color HurtboxColor = ghostSettings.HurtboxColor;

    // we use it so it's not affected by ActualCollideHitbox 

    private void DrawHitbox(float x, float y, float width, float height, Color color) {
        DrawHollowRect(Position.X + x, Position.Y + y, width, height, color);
    }
    private static void DrawHollowRect(float x, float y, float width, float height, Color color) {
        int fx = (int)Math.Floor(x);
        int fy = (int)Math.Floor(y);
        int cw = (int)Math.Ceiling(width + x - fx);
        int cy = (int)Math.Ceiling(height + y - fy);
        OrigHollowRect(fx, fy, cw, cy, color);
    }

    private static Rectangle rect = new Rectangle();
    private static readonly Texture2D texture2d = Monocle.Draw.Pixel.Texture.Texture_Safe;
    private static readonly Rectangle clip = Monocle.Draw.Pixel.ClipRect;
    private static readonly SpriteBatch sb = Monocle.Draw.SpriteBatch;
    private static void OrigHollowRect(int x, int y, int width, int height, Color color) {
        rect.X = x;
        rect.Y = y;
        rect.Width = width;
        rect.Height = 1;
        sb.Draw(texture2d, rect, clip, color);
        rect.Y += height - 1;
        sb.Draw(texture2d, rect, clip, color);
        rect.Y -= height - 1;
        rect.Width = 1;
        rect.Height = height;
        sb.Draw(texture2d, rect, clip, color);
        rect.X += width - 1;
        sb.Draw(texture2d, rect, clip, color);
    }
}