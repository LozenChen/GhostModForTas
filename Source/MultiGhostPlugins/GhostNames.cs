using Celeste.Mod.GhostModForTas.Replayer;
using Celeste.Mod.GhostModForTas.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GhostModForTas.MultiGhost;

public class GhostNames : Component {

    private GhostReplayerEntity parent;

    private Camera camera;

    public GhostNames(GhostReplayerEntity replayer) : base(false, true) {
        parent = replayer;
    }
    public override void Render() {
        if (!ghostSettings.ShowGhostName) {
            return;
        }

        if (Scene is not Level level) {
            return;
        }

        camera ??= level.Camera;

        if (camera is null) {
            return;
        }

        foreach (Ghost ghost in parent.Ghosts) {
            string name = ghost.Name;
            if (string.IsNullOrWhiteSpace(name)) {
                continue;
            }
            Vector2 pos = ModImports.IsActorInverted(ghost) ? ghost.BottomCenter : ghost.Position;
            pos.Y -= 16f;

            pos -= level.Camera.Position;
            pos *= 6f; // 1920 / 320

            Vector2 size = ActiveFont.Measure(name);
            pos = pos.Clamp(
                10f + size.X * f_scale * 0.5f, 0f + size.Y * f_scale,
                1910f - size.X * f_scale * 0.5f, 1080f
            );

            ActiveFont.DrawOutline(
                name,
                pos,
                new Vector2(0.5f, 1f),
                Vector2.One * f_scale,
                Color.White,
                2f,
                Color.Black
            );
        }
    }

    public static float f_scale = 0.5f;
}