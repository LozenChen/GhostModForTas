using Celeste.Mod.GhostModForTas.Replayer;
using Celeste.Mod.GhostModForTas.Utils;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GhostModForTas.MultiGhost;

public class GhostRankingList : Component {

    private GhostReplayerEntity parent;


    public GhostRankingList(GhostReplayerEntity replayer) : base(false, true) {
        parent = replayer;
    }

    public override void Render() {
        // todo
    }

}