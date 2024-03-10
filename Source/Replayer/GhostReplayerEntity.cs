using Celeste.Mod.GhostModForTas.Module;
using Celeste.Mod.GhostModForTas.Recorder.Data;
using Celeste.Mod.GhostModForTas.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.GhostModForTas.Replayer;


internal static class GhostReplayer {

    public static GhostReplayerEntity Replayer;

    [LoadLevel]
    public static void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
        if ((GhostModule.ModuleSettings.Mode & GhostModuleMode.Play) != GhostModuleMode.Play) {
            Replayer?.RemoveSelf();
            return;
        }

        if (LoadLevelDetector.IsStartingLevel(level, isFromLoader)) {
            Replayer?.RemoveSelf();
            level.Add(Replayer = new GhostReplayerEntity(level));
        } else if (!isFromLoader && level.Tracker.GetEntity<GhostReplayerEntity>() is { } replayer) {
            Replayer = replayer;
            Replayer.HandleTransition(level);
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
    public Ghost ComparerGhost;
    public bool ForceSync = false;
    public string RoomName;
    public Dictionary<string, int> RevisitCount = new();
    public readonly static Color ColorGold = new Color(1f, 1f, 0f, 1f);
    public readonly static Color ColorNeutral = new Color(1f, 1f, 1f, 1f);


    public GhostReplayerEntity(Level level)
        : base(Vector2.Zero) {
        ForceSync = ghostSettings.ForceSync;
        Tag = Tags.HUD | Tags.FrozenUpdate | Tags.PauseUpdate | Tags.TransitionUpdate | Tags.Global;

        // Read and add all ghosts.
        GhostData.FindAllGhosts(level.Session).ForEach(ghost => {
            level.Add(ghost);
            Ghosts.Add(ghost);
            ghost.ForceSync = ForceSync;
        });
        if (Ghosts.Count == 0) {
            PostUpdate += RemoveReplayer;
            Active = false;
        } else {
            Ghosts.Sort((x, y) => System.Math.Sign(x.AllRoomData.LastOrDefault().SessionTime - y.AllRoomData.LastOrDefault().SessionTime));
            RoomName = level.Session.Level;
            RevisitCount.Add(RoomName, 1);
            ComparerGhost = Ghosts.FirstOrDefault();
            GhostCompare.ResetCompareTime();
        }

    }

    public override void Update() {
        base.Update();
        foreach (Ghost ghost in Ghosts) {
            ghost.UpdateByReplayer();
        }
    }

    public void HandleTransition(Level level) {
        if (RoomName == level.Session.Level || !Active) {
            return;
        }

        string target = level.Session.Level;
        if (RevisitCount.ContainsKey(target)) {
            RevisitCount[target]++;
        } else {
            RevisitCount.Add(target, 1);
        }
        LevelCount lc = new LevelCount(target, RevisitCount[target]);
        if (ForceSync) {
            foreach (Ghost ghost in Ghosts) {
                ghost.Sync(lc);
            }
            // if we go room A -> B -> D, ghost go A -> C -> D, then we should get noticed that no ghost when we goto room B, and ghost compare work again when we goto room D
            // in this case (No Ghost), ghost compare will not update in room B, so in room D, we get player time = A -> D, ghost time = A -> D, so it still makes sense to compare last room time and total time
            // in another case (GhostChange), ghost compare always work, total diff works well, but last room diff becomes wrong (only in this room)
            if (Ghosts.Where(x => !x.NotSynced).FirstOrDefault() is { } firstGhost) {
                GhostCompare.UpdateRoomTime(level, firstGhost.LastSessionTime);
                if (ComparerGhost != firstGhost) {
                    GhostCompare.Complaint = GhostCompare.ComplaintMode.GhostChange;
                    ComparerGhost = firstGhost;
                }
            } else {
                GhostCompare.Complaint = GhostCompare.ComplaintMode.NoGhost;
            }
        } else {
            bool found = false;
            foreach (Ghost ghost in Ghosts) {
                foreach (GhostData data in ghost.AllRoomData) {
                    if (data.LevelCount == new LevelCount(RoomName, RevisitCount[RoomName]) && data.TargetCount.Level == target) {
                        long time = data.SessionTime;
                        GhostCompare.UpdateRoomTime(level, time);
                        if (ghost != ComparerGhost) {
                            GhostCompare.Complaint = GhostCompare.ComplaintMode.GhostChange;
                            ComparerGhost = ghost;
                        }
                        found = true;
                        break;
                    }
                }
                if (found) {
                    break;
                }
            }
            if (!found) {
                GhostCompare.Complaint = GhostCompare.ComplaintMode.NoGhost;
            }

        }
        RoomName = target;
    }

    public void RemoveReplayer(Entity replayer) {
        RemoveSelf();
        Ghosts.ForEach(ghost => ghost.RemoveSelf());
        Ghosts.Clear();
        ComparerGhost = null;
    }

    public override void Removed(Scene scene) {
        base.Removed(scene);
        Ghosts.ForEach(ghost => ghost.RemoveSelf());
        Ghosts.Clear();
        ComparerGhost = null;
    }


    public override void Render() {
        foreach (Ghost ghost in Ghosts) {
            ghost.Color = ColorNeutral;
        }
        if (ComparerGhost is not null) {
            ComparerGhost.Color = ColorGold;
        }

        base.Render();
    }
}