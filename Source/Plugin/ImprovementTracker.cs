using Celeste.Mod.GhostModForTas.Module;
using Celeste.Mod.GhostModForTas.Recorder.Data;
using Celeste.Mod.GhostModForTas.Replayer;
using Celeste.Mod.GhostModForTas.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Celeste.Mod.GhostModForTas.Plugin;
internal static class ImprovementTracker {

    public static bool IsIGT;

    public enum States { Tracking, TrackLost, Finished }

    public static States State = States.Finished;

    public static bool Tracking => State == States.Tracking;

    public static List<RoomInfo> Diffs = new List<RoomInfo>();

    public static Dictionary<string, int> RevisitCount = new();

    public static string MapName;

    public static string Error = "Not Started";

    public static string SuccessOutput;

    public static string LastSuccessOutput => SuccessOutput ?? Error;


    public struct RoomInfo {
        public long diffRoomTime;
        public LevelCount room;
        public RoomInfo(long time, LevelCount lc) {
            diffRoomTime = time;
            room = lc;
        }

        public string FormatTimeAndLevel() {
            return GhostCompare.FormatTime(diffRoomTime, false, TimeFormats.FrameOnly) + " " + FormatLevel();
        }

        public string FormatLevel() {
            if (RevisitCount.TryGetValue(room.Level, out int count) && count > 1) {
                return $"[{room.Level} ({room.Count})]: ";
            }
            return $"[{room.Level}]: ";
        }
    }

    public static void Start(Level level) {
        State = States.Tracking;
        Diffs = new();
        IsIGT = ghostSettings.IsIGT;

        AreaKey data = level.Session.Area;
        if (data.SID.StartsWith("Celeste/")) {
            int i = data.ID;
            MapName = i switch {
                0 => "Prologue",
                8 => "Epilogue",
                10 => "Farewell",
                < 8 => i.ToString() + data.Mode switch { AreaMode.Normal => "A", AreaMode.BSide => "B", AreaMode.CSide => "C", _ => "" },
                9 => "8" + data.Mode switch { AreaMode.Normal => "A", AreaMode.BSide => "B", AreaMode.CSide => "C", _ => "" },
                _ => "???"
            };
        }
        else {
            MapName = data.SID;
            if (data.Mode != AreaMode.Normal) {
                MapName += data.Mode switch { AreaMode.BSide => ", B-Side", AreaMode.CSide => ", C-Side", _ => "" };
            }
        }
    }

    public static void SetTrackLost(string error) {
        if (Tracking) {
            State = States.TrackLost;
            Error = $"Track Lost ({error})";
        }
    }

    public static void OnConfigChange() {
        SetTrackLost("RTA / IGT mode changed");
    }
    public static void Add(long diffRoomTime, LevelCount lc) {
        if (Tracking && diffRoomTime != 0) {
            Diffs.Add(new RoomInfo(diffRoomTime, lc));
        }
    }

    public static void Finish(Dictionary<string, int> dict) {
        if (Tracking) {
            RevisitCount = dict;
            Format();
            if (Diffs.IsNotEmpty()) {
                Log("\n\n" + SuccessOutput + "\n");
                // add enough blank lines so users won't copy-paste irrelevant text
            } else {
                Log(SuccessOutput);
            }
        }
        else if (State == States.TrackLost){
            Log(Error);
        }
        State = States.Finished;
    }

    public static void Format() {
        StringBuilder sb = new();
        long currentTime = GhostCompare.CurrentTime;
        long ghostTime = GhostCompare.GhostTime;
        long diffTotal = currentTime - ghostTime;
        sb.Append(diffTotal == 0 ? "-0f" : GhostCompare.FormatTime(diffTotal, false, TimeFormats.FrameOnly));
        if (!IsIGT) {
            sb.Append(" RTA");
        }
        sb.Append(' ').Append(MapName);
        sb.Append(" (").Append(RemoveF(GhostCompare.FormatTime(ghostTime, true, TimeFormats.SecondAndFrame)))
          .Append(" -> ").Append(RemoveF(GhostCompare.FormatTime(currentTime, true, TimeFormats.SecondAndFrame))).Append(')');
        if (Diffs.Count == 1) {
            sb.Append('\n').Append(Diffs.First().FormatLevel());
        }
        else {
            foreach (RoomInfo roomInfo in Diffs) {
                sb.Append('\n').Append(roomInfo.FormatTimeAndLevel());
            }
        }
        SuccessOutput = sb.ToString();

        static string RemoveF(string str) {
            return str.Remove(str.Length - 2, 1);
            // 2:48.589(9917f) -> 2:48.589(9917)
        }
    }

    public static void Log(string str) {
        Logger.Log(LogLevel.Verbose, "GhostModForTas", nameof(ImprovementTracker) + ": " + str);
    }
}
