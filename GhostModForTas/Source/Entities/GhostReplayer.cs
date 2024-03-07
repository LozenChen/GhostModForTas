using Celeste.Mod.GhostModForTas.Module;
using Celeste.Mod.GhostModForTas.Recorder.Data;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.GhostModForTas.Entities;


internal static class GhostReplayerLogic {

    public static GhostReplayer GhostReplayer;

    [LoadLevel]
    public static void OnLoadLevel(Level level) {
        if (level.Tracker.GetEntity<GhostReplayer>() is { } replayer) {
            GhostReplayer = replayer;
        } else {
            level.Add(GhostReplayer = new GhostReplayer(level));
        }
    }
}

[Tracked(false)]
public class GhostReplayer : Entity {
    // it's originally named as GhostManager

    public List<Ghost> Ghosts = new List<Ghost>();


    public readonly static Color ColorGold = new Color(1f, 1f, 0f, 1f);
    public readonly static Color ColorNeutral = new Color(1f, 1f, 1f, 1f);

    public GhostReplayer(Level level)
        : base(Vector2.Zero) {

        Tag = Tags.HUD;

        // Read and add all ghosts.
        GhostData.ForAllGhosts(level.Session, (i, ghostData) => {
            Ghost ghost = new Ghost(ghostData);
            level.Add(ghost);
            Ghosts.Add(ghost);
            Logger.Log("Ghost Mod For Tas", $"Play Run: {ghostData.Run}");
            return true;
        });
    }

    public override void Update() {
        base.Update();
        foreach (Ghost ghost in Ghosts) {
            ghost.UpdateByReplayer();
        }
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);

        // Remove any dead ghosts (heh)
        Ghosts.ForEach(ghost => ghost.RemoveSelf());

        Ghosts.Clear();
    }

    public override void Render() {
        /* Proposed colors:
         * blue - full run PB (impossible)
         * silver - chapter PB (feasible)
         * gold - room PB (done)
         */

        // Gold is the easiest: Find fastest active ghost.
        if (GhostModule.ModuleSettings.HighlightFastestGhost) {
            Ghost fastest = null;
            foreach (Ghost ghost in Ghosts) {
                // While we're at it, reset all colors.
                ghost.Color = ColorNeutral;

                if (fastest == null || ghost.Data.Frames.Count < fastest.Data.Frames.Count) {
                    fastest = ghost;
                }
            }

            if (fastest != null) {
                fastest.Color = ColorGold;
            }
        }

        base.Render();
    }
}