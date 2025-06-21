using Celeste.Mod.GhostModForTas.Replayer;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.GhostModForTas.MultiGhost;

public class GhostColors : Component {

    private GhostReplayerEntity parent;
    public GhostColors(GhostReplayerEntity replayer) : base(false, false) {
        parent = replayer;
        HandleTransition();
    }
    public void HandleTransition() {
        if (ghostSettings.RandomizeGhostColors) {
            RandomColorMode();
        } else {
            ClassicMode();
        }
    }

    public void ClassicMode() {
        foreach (Ghost ghost in parent.Ghosts) {
            ghost.Color = ghostSettings.GhostSpriteColor;
        }
        if (parent.ComparerGhost is not null) {
            parent.ComparerGhost.Color = ghostSettings.ComparerGhostSpriteColor;
        }
    }

    public bool Randomized = false;
    public void RandomColorMode() {
        if (!Randomized) {
            Random random = new();
            foreach (Ghost ghost in parent.Ghosts) {
                int r = random.Next(256);
                int g = random.Next(256);
                int b = random.Next(256);
                if (0.299f * r + 0.587f * g + 0.114f * b < 0.4f * 256) {
                    r = 256 - r;
                    g = 256 - g;
                    b = 256 - b;
                }
                ghost.Color = new Color(r, g, b);
            }
            Randomized = true;
        }
    }
}