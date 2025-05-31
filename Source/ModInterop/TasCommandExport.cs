using Celeste.Mod.GhostModForTas.Recorder;
using Celeste.Mod.GhostModForTas.Replayer;
using Celeste.Mod.GhostModForTas.Utils;
using StudioCommunication;
using StudioCommunication.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TAS.Input;

namespace Celeste.Mod.GhostModForTas.ModInterop;

internal static class TasCommandExport {


    [TasCommand("Ghost_StartRecord", MetaDataProvider = typeof(StartRecordMeta), Aliases = new[] { "GhostStartRecord", "StartGhostRecording", "StartGhostRecord", "GhostStartRecording", "GhostRecord", "GhostRecording", "RecordGhost", "RecordingGhost", "GhostRecordMode", "RecordGhostMode" }, ExecuteTiming = ExecuteTiming.Runtime)]
    public static void StartRecordingCommandWrapper(CommandLine commandLine, int studioLine, string filePath, int fileLine) {
        GhostRecorder.StartRecordingCommand();
    }

    [TasCommand("Ghost_StopRecord", MetaDataProvider = typeof(StopRecordMeta), Aliases = new[] { "GhostStopRecord", "StopGhostRecording", "StopGhostRecord", "GhostStopRecording" }, ExecuteTiming = ExecuteTiming.Runtime)]
    public static void StopRecordingCommandWrapper(CommandLine commandLine, int studioLine, string filePath, int fileLine) {
        GhostRecorder.StopRecordingCommand();
    }

    [TasCommand("Ghost_StartReplay", MetaDataProvider = typeof(StartReplayMeta), Aliases = new[] { "GhostStartReplay", "StartGhostReplaying", "StartGhostPlaying", "StartReplayingGhost", "StartPlayingGhost", "StartGhostPlay", "StartReplayGhost", "StartPlayGhost", "GhostPlay", "GhostReplay", "GhostStartPlay", "GhostStartPlaying", "StartGhostReplay", "GhostStartReplaying", "ReplayGhost", "PlayGhost", "GhostPlayMode", "PlayGhostMode", "GhostReplayMode", "ReplayGhostMode" }, ExecuteTiming = ExecuteTiming.Runtime)]
    public static void StartGhostReplayCommandWrapper(CommandLine commandLine, int studioLine, string filePath, int fileLine) {
        GhostRecorder.StartGhostReplayCommand();
    }

    [TasCommand("Ghost_StopReplay", MetaDataProvider = typeof(StopReplayMeta), Aliases = new[] { "GhostStopReplay", "StopGhostPlay", "StopReplayGhost", "StopPlayGhost", "StopGhostReplay", "GhostStopPlay", "StopGhostReplaying", "StopGhostPlaying", "StopReplayingGhost", "StopPlayingGhost", "GhostStopReplaying", "GhostStopPlaying" }, ExecuteTiming = ExecuteTiming.Runtime)]
    public static void StopReplayCommandWrapper(CommandLine commandLine, int studioLine, string filePath, int fileLine) {
        GhostRecorder.StopReplayCommand();
    }


    [TasCommand("Ghost_SetName", MetaDataProvider = typeof(SetNameMeta), ExecuteTiming = ExecuteTiming.Runtime)]
    public static void GhostSetNameWrapper(CommandLine commandLine, int studioLine, string filePath, int fileLine) {
        if (commandLine.Arguments.IsEmpty()) {
            TasImports.AbortTas("Need to specify arguments for \"Ghost_SetName\" command");
            return;
        }
        Recorder.Data.GhostData.SetGhostName(commandLine.Arguments[0]);
    }

    [TasCommand("Ghost_LockComparer", MetaDataProvider = typeof(LockComparerMeta), ExecuteTiming = ExecuteTiming.Runtime)]
    public static void GhostLockComparer(CommandLine commandLine, int studioLine, string filePath, int fileLine) {
        if (commandLine.Arguments.IsEmpty()) {
            TasImports.AbortTas("Need to specify arguments for \"Ghost_LockComparer\" command");
            return;
        }
        GhostReplayer.LockComparerGhostCommand(commandLine.Arguments[0]);
    }

    [TasCommand("Ghost_MarkLevelEnd", MetaDataProvider = typeof(MarkLevelEndMeta), ExecuteTiming = ExecuteTiming.Runtime)]
    public static void GhostMarkLevelEndWrapper(CommandLine commandLine, int studioLine, string filePath, int fileLine) {
        GhostRecorder.ManuallyMarkLevelEnd();
    }

    [TasCommand("Ghost_ReplayForward", MetaDataProvider = typeof(ReplayForwardMeta), ExecuteTiming = ExecuteTiming.Runtime)]
    public static void GhostReplayForward(CommandLine commandLine, int studioLine, string filePath, int fileLine) {
        if (GhostReplayer.Replayer is not { } replayer || replayer.ForceSync) {
            TasImports.AbortTas($"{CreateErrorText()}Ghost_ReplayForward command can't run when ForceSync is ON.");
            return;
        }
        string[] args = commandLine.Arguments;
        if (args.IsEmpty()) {
            TasImports.AbortTas($"{CreateErrorText()}Ghost_ReplayForward command no frame given");
        } else if (!int.TryParse(args[0], out int frame)) {
            TasImports.AbortTas($"{CreateErrorText()}Ghost_ReplayForward command's frame is not an integer");
        } else {
            GhostReplayer.GhostReplayForward(frame);
        }

        string CreateErrorText() => $"{Path.GetFileName(filePath)} line {fileLine}\n";
    }

    [TasCommand("Ghost_Reload", MetaDataProvider = typeof(ReloadMeta), Aliases = new[] { "GhostReload", "GhostReplayReload", "ReloadGhostReplay", "ReloadGhost" }, ExecuteTiming = ExecuteTiming.Runtime)]
    public static void GhostReplayReloadCommandWrapper(CommandLine commandLine, int studioLine, string filePath, int fileLine) {
        GhostRecorder.GhostReplayReloadCommand();
    }


    public class StartRecordMeta : ITasCommandMeta {
        public string Insert => "Ghost_StartRecord";
        public bool HasArguments => false;
    }

    public class StopRecordMeta : ITasCommandMeta {
        public string Insert => "Ghost_StopRecord";
        public bool HasArguments => false;
    }

    public class StartReplayMeta : ITasCommandMeta {
        public string Insert => "Ghost_StartReplay";
        public bool HasArguments => false;
    }

    public class StopReplayMeta : ITasCommandMeta {
        public string Insert => "Ghost_StopReplay";
        public bool HasArguments => false;
    }

    public class ReloadMeta : ITasCommandMeta {
        public string Insert => "Ghost_Reload";
        public bool HasArguments => false;
    }

    public class SetNameMeta : ITasCommandMeta {
        public string Insert => $"Ghost_SetName{CommandInfo.Separator}[0;Name]";

        public bool HasArguments => true;
    }

    public class MarkLevelEndMeta : ITasCommandMeta {
        public string Insert => "Ghost_MarkLevelEnd";

        public bool HasArguments => false;
    }

    public class ReplayForwardMeta : ITasCommandMeta {
        public string Insert => $"Ghost_ReplayForward{CommandInfo.Separator}[0;Frame]";

        public bool HasArguments => true;
    }

    public class LockComparerMeta : ITasCommandMeta {
        public string Insert => $"Ghost_LockComparer{CommandInfo.Separator}[0;GhostName]";

        public bool HasArguments => true;

        public int GetHash(string[] args, string filePath, int fileLine) {
            int hash = args[..Math.Max(0, args.Length - 1)].Aggregate(17, (int current, string arg) => 31 * current + 17 * arg.GetStableHashCode());
            if (GhostReplayer.Replayer?.Ghosts is List<Ghost> ghosts && ghosts.IsNotEmpty()) {
                hash = ghosts.Select(x => x.Name).Distinct().Aggregate(hash, (current, arg) => 31 * current + 17 * arg.GetStableHashCode());
            }
            return hash;
        }

        public IEnumerator<CommandAutoCompleteEntry> GetAutoCompleteEntries(string[] args, string filePath, int fileLine) {
            if (args.Length != 1) {
                yield break;
            }
            while (GhostReplayer.Replayer?.Ghosts.IsNullOrEmpty() ?? true) {
                yield break;
            }

            List<string> nameList = GhostReplayer.Replayer!.Ghosts.Select(x => x.Name).Distinct().ToList();

            foreach (string name in nameList) {
                yield return new CommandAutoCompleteEntry { Name = name };
            }
        }
    }
}