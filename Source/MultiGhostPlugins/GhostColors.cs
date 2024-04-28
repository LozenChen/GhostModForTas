using Celeste.Mod.GhostModForTas.Replayer;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.GhostModForTas.MultiGhost;

public class GhostColors : Component {

    private GhostReplayerEntity parent;

    public readonly static Color ColorGold = new Color(1f, 1f, 0f, 1f);
    public readonly static Color ColorNeutral = new Color(1f, 1f, 1f, 1f);
    public GhostColors(GhostReplayerEntity replayer) : base(true, false) {
        parent = replayer;
    }
    public override void Update() {
        DefaultMode();
        //RandomColorMode();
    }

    public void DefaultMode() {
        foreach (Ghost ghost in parent.Ghosts) {
            ghost.Color = ColorNeutral;
        }
        if (parent.ComparerGhost is not null) {
            parent.ComparerGhost.Color = ColorGold;
        }
    }

    public bool Randomized = false;
    public void RandomColorMode() {
        if (!Randomized) {
            Random random = new();
            foreach (Ghost ghost in parent.Ghosts) {
                ghost.Color = new Color(random.Next(256), random.Next(256), random.Next(256));
            }
            Randomized = true;
        }
    }
}