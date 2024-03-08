using Celeste.Mod.GhostModForTas.Module;
using Celeste.Mod.GhostModForTas.Recorder.Data;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.GhostModForTas.Entities;

public class Ghost : Actor {
    public bool Done;
    public PlayerSprite Sprite;
    public PlayerHair Hair;
    public int MachineState;

    public GhostData Data;
    public List<GhostData> AllRoomData;
    public int CurrentRoomByOrder;
    public int FrameIndex;
    public GhostFrame Frame => Data == null ? default(GhostFrame) : Data[FrameIndex];

    public Color Color = Color.White;


    public Ghost(List<GhostData> allData)
        : base(Vector2.Zero) {
        Tag = Tags.Persistent;
        Active = false;
        AllRoomData = allData;
        Data = AllRoomData[0];
        CurrentRoomByOrder = 0;
        Done = false;
        Depth = 1;
        Sprite = new PlayerSprite(PlayerSpriteMode.MadelineAsBadeline);
        Add(Hair = new PlayerHair(Sprite));
        Add(Sprite); // add it later so it renders above hair
        Hair.Color = Player.NormalBadelineHairColor;
        FrameIndex = -1;
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        Hair.Facing = Frame.ChunkData.Facing;
        Hair.Start();
        UpdateHair();
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
    }

    public void UpdateByReplayer() {
        if (Done) {
            return;
        }
        Visible = (GhostModule.ModuleSettings.Mode & GhostModuleMode.Play) == GhostModuleMode.Play;
        FrameIndex++;

        if (FrameIndex < 0) { // how
            return;
        }
        if (FrameIndex >= Data.Frames.Count) {
            CurrentRoomByOrder++;
            if (CurrentRoomByOrder < AllRoomData.Count) {
                Data = AllRoomData[CurrentRoomByOrder];
                FrameIndex = 0;
            } else {
                Done = true;
                Visible = false;
                return;
            }
        }
        Visible &= Frame.ChunkData.HasPlayer;
        UpdateSprite();
        UpdateHair();
        base.Update();
    }

    public override void Update() {
        // do nothing
    }


    public void UpdateHair() {
        if (!Frame.ChunkData.HasPlayer) {
            return;
        }

        Hair.Facing = Frame.ChunkData.Facing;
        Hair.SimulateMotion = Frame.ChunkData.HairSimulateMotion;
        if (Frame.ChunkData.UpdateHair) {
            Hair.AfterUpdate();
        }
    }

    public void UpdateSprite() {
        if (!Frame.ChunkData.HasPlayer) {
            return;
        }

        Position = Frame.ChunkData.Position;
        Sprite.Rotation = Frame.ChunkData.Rotation;
        Sprite.Scale = Frame.ChunkData.Scale;
        Sprite.Scale.X = Sprite.Scale.X * (float)Frame.ChunkData.Facing;
        Sprite.Color = new Color(
            (Frame.ChunkData.Color.R * Color.R) / 255,
            (Frame.ChunkData.Color.G * Color.G) / 255,
            (Frame.ChunkData.Color.B * Color.B) / 255,
            (Frame.ChunkData.Color.A * Color.A) / 255
        );

        Sprite.Rate = Frame.ChunkData.SpriteRate;
        Sprite.Justify = Frame.ChunkData.SpriteJustify;
        Sprite.HairCount = Frame.ChunkData.HairCount;

        try {
            if (Sprite.CurrentAnimationID != Frame.ChunkData.CurrentAnimationID) {
                Sprite.Play(Frame.ChunkData.CurrentAnimationID);
            }

            Sprite.SetAnimationFrame(Frame.ChunkData.CurrentAnimationFrame);
        } catch {
            // Play likes to fail randomly as the ID doesn't exist in an underlying dict.
            // Let's ignore this for now.
        }
    }
}