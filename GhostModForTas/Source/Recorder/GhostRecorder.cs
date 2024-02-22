using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GhostModForTas.Recorder;

public class GhostRecorder : Entity {
    public Player Player;

    public GhostData Data;

    public GhostFrame LastFrameData;

    public GhostRecorder(Player player)
        : base() {
        Player = player;
        Depth = 1000000;

        Tag = Tags.HUD;
    }

    public override void Update() {
        base.Update();

        RecordData();
    }

    public void RecordData() {
        if (Player == null) {
            return;
        }

        Session session = (Engine.Scene as Level).Session;

        // A data frame is always a new frame, no matter if the previous one lacks data or not.
        LastFrameData = new GhostFrame {
            Data = new GhostChunkData {
                IsValid = true,

                InControl = Player.InControl,

                Position = Player.Position,
                Speed = Player.Speed,
                Rotation = Player.Sprite.Rotation,
                Scale = Player.Sprite.Scale,
                Color = Player.Sprite.Color,

                Facing = Player.Facing,

                CurrentAnimationID = Player.Sprite.CurrentAnimationID,
                CurrentAnimationFrame = Player.Sprite.CurrentAnimationFrame,

                HairColor = Player.Hair.Color,
                HairSimulateMotion = Player.Hair.SimulateMotion,

                DashColor = Player.StateMachine.State == Player.StDash ? Player.GetCurrentTrailColor() : (Color?)null,
                DashDir = Player.DashDir,
                DashWasB = Player.wasDashB,

                Time = session.Time
            }
        };

        if (Player.StateMachine.State == Player.StRedDash) {
            LastFrameData.Data.HairCount = 1;
        } else if (Player.StateMachine.State != Player.StStarFly) {
            LastFrameData.Data.HairCount = Player.Dashes > 1 ? 5 : 4;
        } else {
            LastFrameData.Data.HairCount = 7;
        }

        if (Data != null) {
            Data.Frames.Add(LastFrameData);
        }
    }
}