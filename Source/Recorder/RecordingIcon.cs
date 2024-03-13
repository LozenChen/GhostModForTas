using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GhostModForTas.Recorder;

internal class RecordingIcon : Entity {
    public static MTexture theo;

    public static MTexture texture;

    public static float alpha = 0.5f;

    public static float scale = 0.8f;

    public static RecordingIcon Instance;
    public RecordingIcon() {
        Position = new Vector2(1920f, 1080f);
        Tag = Tags.HUD;
        Update();
        Visible &= ghostSettings.Mode.HasFlag(Module.GhostModuleMode.Record);
        Instance = this;
    }

    [LoadLevel]
    public static void OnLoadLevel(Level level) {
        level.Add(new RecordingIcon());
    }

    public override void Update() {
        base.Update();
        Visible = ghostSettings.ShowRecorderIcon && GhostRecorder.Recorder is not null;
    }

    public override void Render() {
        texture.DrawJustified(Position, Vector2.One, Color.White * alpha, 0.5f * scale);
        theo.DrawJustified(Position - Vector2.UnitY * 20f * scale, Vector2.One, Color.Yellow * alpha, 3f * scale);
    }

    [Initialize]
    public static void Initiailize() {
        theo = GFX.Game["GhostModForTas/recorder"];
        texture = GFX.Game["GhostModForTas/recorder2"];
    }

}