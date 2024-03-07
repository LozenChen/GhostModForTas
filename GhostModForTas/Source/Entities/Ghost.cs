using Celeste.Mod.GhostModForTas.Module;
using Celeste.Mod.GhostModForTas.Recorder.Data;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GhostModForTas.Entities;

public class Ghost : Actor {
    public GhostReplayer Manager;
    public PlayerSprite Sprite;
    public PlayerHair Hair;
    public int MachineState;

    public GhostData Data;
    public int FrameIndex = 0;
    public GhostFrame PrevFrame => Data == null ? default(GhostFrame) : Data[FrameIndex - 1];
    public GhostFrame Frame => Data == null ? default(GhostFrame) : Data[FrameIndex];

    public Color Color = Color.White;


    public Ghost(GhostData data)
        : base(Vector2.Zero) {
        Data = data;

        Depth = 1;
        // Tag = Tags.PauseUpdate;
        Sprite = new PlayerSprite(PlayerSpriteMode.MadelineAsBadeline);
        Add(Hair = new PlayerHair(Sprite));
        Add(Sprite); // add it later so it renders above hair
        Hair.Color = Player.NormalBadelineHairColor;

        // Hair.AfterUpdate is not called when Ghost.Active is false, so we can't set it to false
    }

    public override void Added(Scene scene) {
        base.Added(scene);

        Hair.Facing = Frame.Data.Facing;
        Hair.Start();
        UpdateHair();
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
    }

    public void UpdateByReplayer() {
        Visible = (GhostModule.ModuleSettings.Mode & GhostModuleMode.Play) == GhostModuleMode.Play;
        Visible &= Frame.Data.HasPlayer;

        if (Data != null) {
            do {
                FrameIndex++;
            } while (
                !PrevFrame.Data.HasPlayer && FrameIndex < Data.Frames.Count // Skip any frames not containing the data chunk.
            );
        }

        UpdateSprite();
        UpdateHair();

        base.Update();
    }
    public override void Update() {
        // do nothing
    }


    public void UpdateHair() {
        if (!Frame.Data.HasPlayer) {
            return;
        }

        Hair.Facing = Frame.Data.Facing;
        Hair.SimulateMotion = Frame.Data.HairSimulateMotion;
    }

    public void UpdateSprite() {
        if (!Frame.Data.HasPlayer) {
            return;
        }

        Position = Frame.Data.Position;
        Sprite.Rotation = Frame.Data.Rotation;
        Sprite.Scale = Frame.Data.Scale;
        Sprite.Scale.X = Sprite.Scale.X * (float)Frame.Data.Facing;
        Sprite.Color = new Color(
            (Frame.Data.Color.R * Color.R) / 255,
            (Frame.Data.Color.G * Color.G) / 255,
            (Frame.Data.Color.B * Color.B) / 255,
            (Frame.Data.Color.A * Color.A) / 255
        );

        Sprite.Rate = Frame.Data.SpriteRate;
        Sprite.Justify = Frame.Data.SpriteJustify;
        Sprite.HairCount = Frame.Data.HairCount;

        try {
            if (Sprite.CurrentAnimationID != Frame.Data.CurrentAnimationID) {
                Sprite.Play(Frame.Data.CurrentAnimationID);
            }

            Sprite.SetAnimationFrame(Frame.Data.CurrentAnimationFrame);
        } catch {
            // Play likes to fail randomly as the ID doesn't exist in an underlying dict.
            // Let's ignore this for now.
        }
    }
}