using Celeste.Mod.GhostModForTas.Replayer;
using Celeste.Mod.GhostModForTas.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GhostModForTas.MultiGhost;

public class GhostNames : Component {

    private GhostReplayerEntity parent;

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

        float alpha = base_alpha;
        if (level.Paused) {
            alpha *= 0.5f;
        }

        foreach (Ghost ghost in parent.Ghosts) {
            string name = ghost.Name;
            if (string.IsNullOrWhiteSpace(name)) {
                continue;
            }
            Vector2 pos = ModImports.IsActorInverted(ghost) ? ghost.BottomCenter : ghost.Position;
            DrawName(pos, name, ghost.Color);
        }

        if (level.Tracker.GetEntity<Player>() is { } player) {
            string name2 = ghostSettings.PlayerName;
            if (!string.IsNullOrWhiteSpace(name2)) {
                Vector2 pos2 = ModImports.IsPlayerInverted ? player.BottomCenter : player.Position;
                DrawName(pos2, name2, Color.White);
            }
        }

        void DrawName(Vector2 pos, string name, Color color) {
            pos.Y -= 16f;

            pos = level.WorldToScreenExt(pos); // supports MirrorMode, UpsideDown, CenterCamera, but doesn't support the ZoomLevel variant from ExtendedVariantMode.

            Vector2 size = ActiveFont.Measure(name);

            float isNotUpsideDown = ModImports.UpsideDown ? 0f : 1f;
            pos = pos.Clamp(
                10f + size.X * f_scale * 0.5f, 0f + isNotUpsideDown * size.Y * f_scale,
                1910f - size.X * f_scale * 0.5f, 1080f - (1f - isNotUpsideDown) * size.Y * f_scale
            );

            ActiveFont.DrawOutline(
                name,
                pos,
                new Vector2(0.5f, isNotUpsideDown),
                Vector2.One * f_scale,
                color * alpha,
                2f,
                Color.Black * (alpha * alpha * alpha)
            );
        }
    }

    public static float f_scale = 0.5f;

    public static float base_alpha = 1f;
}