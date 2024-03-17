# GhostModForTas

### License: MIT

----

This Celeste mod is based on DemoJameson's [GhostMod](https://github.com/DemoJameson/GhostMod), which is a fork of Everest Team's [GhostMod](https://github.com/EverestAPI/GhostMod).

https://gamebanana.com/mods/500759

## How to use

Step1: Switch to RECORD mode manually, or use a StartGhostRecording command before the first frame in the tas file.

Step2: RESTART chapter or use a CONSOLE LOAD command to start your run to record. If you succeed, you will find a Recording icon in the bottom-right of the screen.

Step3: Whenever you finishes a room, a Ghost file will be written to storage.

Step4: Wait until your tas finishes, or use a StopGhostRecording command, to stop recording. Note that if the last recording room is not finished, then it will be dropped.

Step5: Switch to PLAY mode manually, or use a StartGhostReplay command in the same position where the StartGhostRecording command was, and enter the same level, to play your records ("Ghosts").

## Force Sync

- If Force Sync is ON, then the ghost will sync with you when you go to next room. The Ghost will be waiting for you if she is faster, or will catch up if she is slower.

## Ghost Info HUD and Ghost Custom Info

- You can drag and move it.

- Ghost info HUD = the TAS Info HUD when you record the ghosts. So if you closes TAS Info HUD when recording ghosts, then Ghost info HUD will be empty.

- Ghost custom info are calculated when you record the ghosts. So when you are playing the Ghost files, changing the Custom Info Template will not provide you more info.

## Ghost Compare Time

- If you change the route (e.g. enter a room that the ghost doesn't enter, and vice versa), then the comparing time system will refuse to show the comparison.

## Hotkeys

- You can use hotkeys to switch modes. It's configurable in the in-game menu.

## Commands

- You can use "ghost_off", "ghost_record", "ghost_play" to switch modes in console.

## TasCommands

- A TasCommand is a command that only works in tas files.

- You can use ***"StartGhostRecording"*** to switch to RECORD mode in tas.

Alias names include "GhostStartRecord", "StartGhostRecord", "GhostStartRecording", "GhostRecord", "GhostRecording", "RecordGhost", "RecordingGhost", "GhostRecordMode" and "RecordGhostMode".

- You can use ***"StopGhostRecording"*** to stop recording.

Though it's easier to do that by just stopping the tas.

Alias names include "GhostStopRecord", "StopGhostRecord" and "GhostStopRecording".

- You can use ***"StartGhostReplay"*** to switch in PLAY mode in tas.

Alias names include "StartGhostReplaying", "StartGhostPlaying", "StartReplayingGhost", "StartPlayingGhost", "StartGhostPlay", "StartReplayGhost", "StartPlayGhost" , "GhostPlay", "GhostReplay", "GhostStartPlay", "GhostStartPlaying", "GhostStartReplay", "GhostStartReplaying", "ReplayGhost", "PlayGhost", "GhostPlayMode", "PlayGhostMode", "GhostReplayMode" and "ReplayGhostMode".

- You can use ***"StopGhostReplay"*** to stop replay.

Alias names include "StopGhostPlay", "StopReplayGhost", "StopPlayGhost", "GhostStopReplay", "GhostStopPlay", "StopGhostReplaying", "StopGhostPlaying", "StopReplayingGhost", "StopPlayingGhost", "GhostStopReplaying" and "GhostStopPlaying".

- "StartGhostRecording" and "StartGhostReplay" should be before the first frame in the tas file. Or at least, they should be put in the same position of a tas file. Otherwise, you will find that the Ghost starts running earlier/later than it should be.

- When tas ends, the mode will return to PLAY or OFF mode (if it's changed by a TasCommand), depending on its original value. It will never return to RECORD mode.

## Issues

- There may be some incompatibility with savestates. Please let me know.