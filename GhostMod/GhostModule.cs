using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;
using EventInstance = FMOD.Studio.EventInstance;

namespace Celeste.Mod.Ghost;

public class GhostModule : EverestModule {
    public static GhostModule Instance;

    public override Type SettingsType => typeof(GhostModuleSettings);
    public static GhostModuleSettings ModuleSettings => (GhostModuleSettings) Instance._Settings;

    public static bool SettingsOverridden = false;

    public static string PathGhosts { get; internal set; }

    public GhostManager GhostManager;
    public GhostRecorder GhostRecorder;

    public Guid Run;


    private long ghostTime;
    private long lastGhostTime;
    private long currentTime;
    private long lastCurrentTime;

    public GhostModule() {
        Instance = this;
    }

    public override void Load() {
        PathGhosts = Path.Combine(Everest.PathSettings, "Ghosts");
        if (!Directory.Exists(PathGhosts)) {
            Directory.CreateDirectory(PathGhosts);
        }

        On.Celeste.Level.LoadLevel += OnLoadLevel;
        Everest.Events.Level.OnExit += OnExit;
        On.Celeste.Player.Die += OnDie;
        On.Celeste.Level.Render += LevelOnRender;
        On.Celeste.Level.NextLevel += LevelOnNextLevel;
        On.Celeste.Level.RegisterAreaComplete += LevelOnRegisterAreaComplete;
        On.Celeste.LevelLoader.ctor += LevelLoaderOnCtor;
    }

    public override void Unload() {
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
        Everest.Events.Level.OnExit -= OnExit;
        On.Celeste.Player.Die -= OnDie;
        On.Celeste.Level.Render -= LevelOnRender;
        On.Celeste.Level.NextLevel -= LevelOnNextLevel;
        On.Celeste.Level.RegisterAreaComplete -= LevelOnRegisterAreaComplete;
        On.Celeste.LevelLoader.ctor -= LevelLoaderOnCtor;
    }

    public void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
        orig(level, playerIntro, isFromLoader);

        if (isFromLoader) {
            GhostManager?.RemoveSelf();
            GhostManager = null;
            GhostRecorder?.RemoveSelf();
            GhostRecorder = null;
            Run = Guid.NewGuid();
        }

        Player player = level.Tracker.GetEntity<Player>();
        if (player == null) {
            level.Add(new Entity {new Coroutine(WaitForPlayer(level))});
        } else {
            Step(level);
        }
    }

    private IEnumerator WaitForPlayer(Level level) {
        while (level.Tracker.GetEntity<Player>() == null) {
            yield return null;
        }

        Step(level);
    }

    public void OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
        if (mode == LevelExit.Mode.Completed ||
            mode == LevelExit.Mode.CompletedInterlude) {
            Step(level);
        }
    }

    public void Step(Level level) {
        if (ModuleSettings.Mode == GhostModuleMode.Off) {
            return;
        }

        string target = level.Session.Level;
        Logger.Log("ghost", $"Stepping into {level.Session.Area.GetSID()} {target}");

        // Write the ghost, even if we haven't gotten an IL PB.
        // Maybe we left the level prematurely earlier?
        if (GhostRecorder?.Data != null &&
            (ModuleSettings.Mode & GhostModuleMode.Record) == GhostModuleMode.Record) {
            GhostRecorder.Data.Target = target;
            GhostRecorder.Data.Run = Run;
            GhostRecorder.Data.Write();
        }

        GhostManager?.RemoveSelf();

        Player player = level.Tracker.GetEntity<Player>();
        level.Add(GhostManager = new GhostManager(player, level));

        if (GhostRecorder != null) {
            GhostRecorder.RemoveSelf();
        }

        level.Add(GhostRecorder = new GhostRecorder(player));
        GhostRecorder.Data = new GhostData(level.Session);
        GhostRecorder.Data.Name = ModuleSettings.Name;
    }

    public PlayerDeadBody OnDie(On.Celeste.Player.orig_Die orig, Player player, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats) {
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

    private void LevelOnNextLevel(On.Celeste.Level.orig_NextLevel orig, Level self, Vector2 at, Vector2 dir) {
        orig(self, at, dir);
        if (GhostManager?.Ghosts.FirstOrDefault()?.Data.Frames.LastOrDefault().Data.Time is long time) {
            lastGhostTime = ghostTime;
            ghostTime = time;
            lastCurrentTime = currentTime;
            currentTime = self.Session.Time;
        }
    }

    private void LevelOnRegisterAreaComplete(On.Celeste.Level.orig_RegisterAreaComplete orig, Level self) {
        orig(self);

        if (GhostManager?.Ghosts.FirstOrDefault()?.Data.Frames.LastOrDefault().Data.Time is long time) {
            lastGhostTime = ghostTime;
            ghostTime = time;
            lastCurrentTime = currentTime;
            currentTime = self.Session.Time;
        }
    }

    private void LevelOnRender(On.Celeste.Level.orig_Render orig, Level self) {
        orig(self);

        if (ModuleSettings.Mode == GhostModuleMode.Play && ModuleSettings.ShowCompareTime) {
            int viewWidth = Engine.ViewWidth;
            int viewHeight = Engine.ViewHeight;

            float pixelScale = viewWidth / 320f;
            float margin = 2 * pixelScale;
            float padding = 2 * pixelScale;
            float fontSize = 0.3f * pixelScale;
            float alpha = 1f;

            if (ghostTime == 0) {
                return;
            }

            long diffRoomTime = currentTime - ghostTime - lastCurrentTime + lastGhostTime;
            long diffTotalTime = currentTime - ghostTime;
            string diffRoomTimeStr = (diffRoomTime > 0 ? "+" : string.Empty) + (diffRoomTime / 10000000D).ToString("0.000");
            string diffTotalTimeStr = (diffTotalTime > 0 ? "+" : string.Empty) + (diffTotalTime / 10000000D).ToString("0.000");
            string timeStr = $"last room: {diffRoomTimeStr}\ntotal    : {diffTotalTimeStr}";

            if (string.IsNullOrEmpty(timeStr)) {
                return;
            }

            Vector2 size = Draw.DefaultFont.MeasureString(timeStr) * fontSize;

            float x;
            float y;

            x = margin;
            y = margin;

            if (Settings.Instance.SpeedrunClock == SpeedrunType.Chapter) {
                y += 16 * pixelScale;
            } else if (Settings.Instance.SpeedrunClock == SpeedrunType.File) {
                y += 20 * pixelScale;
            }

            Rectangle bgRect = new Rectangle((int) x, (int) y, (int) (size.X + padding * 2), (int) (size.Y + padding * 2));

            if (self.Entities.FindFirst<Player>() is Player player) {
                Vector2 playerPosition = self.Camera.CameraToScreen(player.TopLeft) * pixelScale;
                Rectangle playerRect = new Rectangle((int) playerPosition.X, (int) playerPosition.Y, (int) (8 * pixelScale), (int) (11 * pixelScale));
                Rectangle mirrorBgRect = bgRect;
                if (SaveData.Instance?.Assists.MirrorMode == true) {
                    mirrorBgRect.X = (int) Math.Abs(x - viewWidth + size.X + padding * 2);
                }

                if (self.Paused || playerRect.Intersects(mirrorBgRect)) {
                    alpha = 0.5f;
                }
            }

            Draw.SpriteBatch.Begin();

            Draw.Rect(bgRect, Color.Black * 0.8f * alpha);

            Vector2 textPosition = new Vector2(x + padding, y + padding);
            Vector2 scale = new Vector2(fontSize);

            Draw.Text(Draw.DefaultFont, timeStr, textPosition, Color.White * alpha, Vector2.Zero, scale, 0f);

            Draw.SpriteBatch.End();
        }
    }

    private void LevelLoaderOnCtor(On.Celeste.LevelLoader.orig_ctor orig, LevelLoader self, Session session, Vector2? startPosition) {
        orig(self, session, startPosition);

        ghostTime = 0;
        lastGhostTime = 0;
        currentTime = 0;
        lastCurrentTime = 0;
    }

    public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot) {
        if (SettingsOverridden && !ModuleSettings.AlwaysShowSettings) {
            menu.Add(new TextMenu.SubHeader(Dialog.Clean("modoptions_ghostmodule_overridden") + " | v." + Metadata.VersionString));
            return;
        }

        base.CreateModMenuSection(menu, inGame, snapshot);
    }
}