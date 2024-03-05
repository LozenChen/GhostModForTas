using Celeste.Mod.GhostModForTas.Entities;
using Celeste.Mod.GhostModForTas.Recorder;
using Celeste.Mod.GhostModForTas.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Celeste.Mod.GhostModForTas.Module;

internal static class GhostCore {

    public static string PathGhosts { get; internal set; }

    public static GhostReplayer GhostReplayer;
    public static GhostRecorder GhostRecorder;

    public static Guid Run;

    [Load]
    public static void Load() {
        PathGhosts = Path.Combine(Everest.PathSettings, "GhostsForTas");
        if (!Directory.Exists(PathGhosts)) {
            Directory.CreateDirectory(PathGhosts);
        }

        On.Celeste.Level.LoadLevel += OnLoadLevel;
        On.Celeste.Player.Die += OnDie;


        typeof(LevelExit).GetConstructor(new Type[] { typeof(LevelExit.Mode), typeof(Session), typeof(HiresSnow) }).IlHook(il => {
            ILCursor cursor = new(il);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate(OnExit);
        });
    }

    [Unload]
    public static void Unload() {
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
        On.Celeste.Player.Die -= OnDie;
    }

    public static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
        orig(level, playerIntro, isFromLoader);

        if (isFromLoader) {
            GhostReplayer?.RemoveSelf();
            GhostReplayer = null;
            GhostRecorder?.RemoveSelf();
            GhostRecorder = null;
            Run = Guid.NewGuid();
        }

        Player player = level.Tracker.GetEntity<Player>();
        if (player == null) {
            level.Add(new Entity { new Coroutine(WaitForPlayer(level)) });
        } else {
            Step(level);
        }
    }

    private static IEnumerator WaitForPlayer(Level level) {
        while (level.Tracker.GetEntity<Player>() == null) {
            yield return null;
        }

        Step(level);
    }

    public static void OnExit(LevelExit.Mode mode) {
        if (Engine.Scene is not Level level) {
            return;
        }
        if (mode == LevelExit.Mode.Completed ||
            mode == LevelExit.Mode.CompletedInterlude) {
            Step(level);
        }
    }

    public static void Step(Level level) {
        if (ghostSettings.Mode == GhostModuleMode.Off) {
            return;
        }

        string target = level.Session.Level;
        Logger.Log("ghost", $"Stepping into {level.Session.Area.GetSID()} {target}");

        // Write the ghost, even if we haven't gotten an IL PB.
        // Maybe we left the level prematurely earlier?
        if (GhostRecorder?.Data != null &&
            (ghostSettings.Mode & GhostModuleMode.Record) == GhostModuleMode.Record) {
            GhostRecorder.Data.Target = target;
            GhostRecorder.Data.Run = Run;
            GhostRecorder.Data.Write();
        }

        GhostReplayer?.RemoveSelf();

        Player player = level.Tracker.GetEntity<Player>();
        level.Add(GhostReplayer = new GhostReplayer(player, level));

        if (GhostRecorder != null) {
            GhostRecorder.RemoveSelf();
        }

        level.Add(GhostRecorder = new GhostRecorder(player));
        GhostRecorder.Data = new GhostData(level.Session);
        GhostRecorder.Data.Name = ghostSettings.Name;
    }

    public static PlayerDeadBody OnDie(On.Celeste.Player.orig_Die orig, Player player, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats) {
        PlayerDeadBody corpse = orig(player, direction, evenIfInvincible, registerDeathInStats);

        if (GhostRecorder == null || GhostRecorder.Data == null) {
            return corpse;
        }

        // This is hacky, but it works:
        // Check the stack trace for Celeste.Level+* <Pause>*
        // and throw away the data when we're just retrying.
        foreach (StackFrame frame in new StackTrace().GetFrames()) {
            MethodBase method = frame?.GetMethod();
            if (method == null || method.DeclaringType == null) {
                continue;
            }

            if (!method.DeclaringType.FullName.StartsWith("Celeste.Level+") ||
                !method.Name.StartsWith("<Pause>")) {
                continue;
            }

            GhostRecorder.Data = null;
            return corpse;
        }

        GhostRecorder.Data.Dead = true;

        return corpse;
    }
}
