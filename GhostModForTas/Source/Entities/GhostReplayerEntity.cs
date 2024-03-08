using Celeste.Mod.GhostModForTas.Module;
using Celeste.Mod.GhostModForTas.Recorder.Data;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.GhostModForTas.Entities;


internal static class GhostReplayer {

    public static GhostReplayerEntity Replayer;

    [LoadLevel]
    public static void OnLoadLevel(Level level) {
        if (level.Tracker.GetEntity<GhostReplayerEntity>() is { } replayer) {
            Replayer = replayer;
            Replayer.HandleTransition(level);
        } else {
            level.Add(Replayer = new GhostReplayerEntity(level));
        }
    }

    [FreezeUpdate]

    public static void UpdateInFreezeFrame() {
        Replayer?.Update();
    }

}

[Tracked(false)]
public class GhostReplayerEntity : Entity {
    // it's originally named as GhostManager

    public List<Ghost> Ghosts = new List<Ghost>();
    public bool ForceSync = false;
    public string RoomName;
    public Dictionary<string, int> RevisitCount = new();
    public readonly static Color ColorGold = new Color(1f, 1f, 0f, 1f);
    public readonly static Color ColorNeutral = new Color(1f, 1f, 1f, 1f);

    public GhostReplayerEntity(Level level)
        : base(Vector2.Zero) {
        ForceSync = ghostSettings.ForceSync;
        Tag = Tags.HUD | Tags.FrozenUpdate | Tags.PauseUpdate | Tags.TransitionUpdate | Tags.Persistent;

        // Read and add all ghosts.
        GhostData.FindAllGhosts(level.Session).ForEach(ghost => {
            level.Add(ghost);
            Ghosts.Add(ghost);
            ghost.ForceSync = ForceSync;
        });
        RoomName = level.Session.Level;
        RevisitCount.Add(RoomName, 1);
    }

    public override void Update() {
        base.Update();
        foreach (Ghost ghost in Ghosts) {
            ghost.UpdateByReplayer();
        }
    }

    public void HandleTransition(Level level) {
        if (RoomName == level.Session.Level) {
            return;
        }

        string target = level.Session.Level;
        if (ForceSync) {
            foreach (Ghost ghost in Ghosts) {
                ghost.Sync();
            }

            if (Ghosts?.FirstOrDefault() is { } firstGhost) {
                if (firstGhost.Data.Level == target && firstGhost.LastSessionTime is { } time) {
                    GhostCompare.UpdateRoomTime(level, time);
                } else {
                    ComplainAboutRouteChange();
                }
            }
        } else {
            if (Ghosts?.FirstOrDefault()?.AllRoomData is { } list) {
                bool found = false;
                foreach (GhostData data in list) {
                    if (data.Level == RoomName && data.LevelVisitCount == RevisitCount[RoomName] && data.Target == target) {
                        long time = data.SessionTime;
                        GhostCompare.UpdateRoomTime(level, time);
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    ComplainAboutRouteChange();
                }
            }
        }
        RoomName = target;
        if (RevisitCount.ContainsKey(RoomName)) {
            RevisitCount[RoomName]++;
        } else {
            RevisitCount.Add(RoomName, 1);
        }
    }

    public static void ComplainAboutRouteChange() {

    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
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