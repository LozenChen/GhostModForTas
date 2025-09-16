using Celeste.Mod.GhostModForTas.ModInterop;
using Celeste.Mod.GhostModForTas.Recorder.Data;
using Celeste.Mod.GhostModForTas.Replayer;
using Celeste.Mod.GhostModForTas.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAS;

namespace Celeste.Mod.GhostModForTas.Plugin;
internal static class ImprovementTracker {

    public static bool IsIGT;

    public enum States { Tracking, TrackLost, Finished }

    public static States State = States.Finished;

    public static bool Tracking => State == States.Tracking;

    public static List<RoomInfo> Diffs = new();

    public static Dictionary<string, int> RevisitCount = new();

    public static string MapName;

    public static string Error = "Not Started";

    public static long TotalTime;

    public static long TotalGhostTime;

    public static string SuccessOutput;

    public static string LastSuccessOutput => SuccessOutput ?? Error;

    public static event Action<string> OnOutput;


    public struct RoomInfo {
        public long diffRoomTime;
        public LevelCount room;
        public RoomInfo(long time, LevelCount lc) {
            diffRoomTime = time;
            room = lc;
        }

        public string FormatTimeAndLevel() {
            return GhostCompare.FormatTime(diffRoomTime, false, "FrameOnly") + " " + FormatLevel();
        }

        public string FormatLevel() {
            if (RevisitCount.TryGetValue(room.Level, out int count) && count > 1) {
                return $"[{room.Level} ({room.Count})]: ";
            }
            return $"[{room.Level}]: ";
        }
    }

    internal static void Start(Level level) {
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

    internal static void SetTrackLost(string error) {
        if (Tracking) {
            State = States.TrackLost;
            Error = $"Track Lost ({error})";
        }
    }

    internal static void OnConfigChange() {
        SetTrackLost("RTA / IGT mode changed");
    }
    internal static void Add(long diffRoomTime, LevelCount lc) {
        if (Tracking && diffRoomTime != 0) {
            Diffs.Add(new RoomInfo(diffRoomTime, lc));
        }
    }

    internal static void Finish(Dictionary<string, int> dict) {
        if (Tracking) {
            RevisitCount = dict;
            TotalTime = GhostCompare.CurrentTime;
            TotalGhostTime = GhostCompare.GhostTime;
            Output();

            /*
             * tas's RealTime command is unstable currently, we do it later
            if (!Manager.Running) {
                Output();
            }
            // else, wait OnTasDiableRun()
            // and inside these function, we do something to sync with tas
            */
        }
        else if (State == States.TrackLost){
            Log(Error);
        }
        State = States.Finished;
    }

    private static void Output() {
        Format();
        if (Diffs.IsNotEmpty()) {
            Log("\n\n" + SuccessOutput + "\n");
            // add enough blank lines so users won't copy-paste irrelevant text
        } else {
            Log(SuccessOutput);
        }
        TasImports.PopupMessageToStudio("GhostModForTas", SuccessOutput);
        OnOutput?.Invoke(SuccessOutput);
    }

    private static void Format() {
        StringBuilder sb = new();
        long diffTotal = TotalTime - TotalGhostTime;
        sb.Append(diffTotal == 0 ? "-0f" : GhostCompare.FormatTime(diffTotal, false, "FrameOnly"));
        if (!IsIGT) {
            sb.Append(" RTA");
        }
        sb.Append(' ').Append(MapName);
        if (IsIGT) {
            sb.Append(" (").Append(GhostCompare.FormatTime(TotalGhostTime, true, "TotalSecondsAndFrames"))
                  .Append(" -> ").Append(GhostCompare.FormatTime(TotalTime, true, "TotalSecondsAndFrames")).Append(')');
        }
        else {
            sb.Append(" (").Append(GhostCompare.FormatTime(TotalGhostTime, true, "TotalSecondsAndFramesRTA"))
                  .Append(" -> ").Append(GhostCompare.FormatTime(TotalTime, true, "TotalSecondsAndFramesRTA")).Append(')');
        }
        if (Diffs.Count == 1) {
            sb.Append('\n').Append(Diffs.First().FormatLevel());
        }
        else {
            foreach (RoomInfo roomInfo in Diffs) {
                sb.Append('\n').Append(roomInfo.FormatTimeAndLevel());
            }
        }
        SuccessOutput = sb.ToString();
    }

    private static void Log(string str) {
        Logger.Log(LogLevel.Verbose, "GhostModForTas", nameof(ImprovementTracker) + ": " + str);
    }
}
