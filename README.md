# GhostModForTas

### License: MIT

----

This Celeste mod is based on DemoJameson's [GhostMod](https://github.com/DemoJameson/GhostMod), which is a fork of Everest Team's [GhostMod](https://github.com/EverestAPI/GhostMod).

This mod aims to record TASes, then render them as ghost replays. So you can compare your TAS against the ghost replays. It's useful when you want to improve a TAS.

https://gamebanana.com/mods/500759

## How to use

Step1: Switch to `RECORD` mode manually, or use a `Ghost_StartRecord` command in Studio to start your record. If you succeed, you will find a Recording icon in the bottom-right of the screen.

Step2: Whenever you finishes a room, a Ghost file will be written to storage.

Step3: Wait until your tas finishes, or use a `Ghost_StopRecord` command, to stop recording. Note that if the last recording room is not finished, then it will be dropped.

Step4: Switch to `PLAY` mode manually and restart chapter / re-run the tas, or use a `Ghost_StartReplay` command in tas file, to play your records ("Ghosts").

## Modes

- You have 4 ways to switch modes: in mod menu, using hotkeys, using tas commands, and using console commands.

### Record Mode

- You can start recording using any of the 4 ways.

### Play Mode

- If you start the chapter while not in `PLAY` mode, then ghosts will not be loaded. Note that loading ghosts takes much time, so even if you switch to `PLAY` mode later using the first three ways, ghosts will still be unloaded. Unless you use the tas command `Ghost_StartReplay`.

- But once the ghosts are loaded, they will persist even if you are no longer in `PLAY` mode (they just become invisible), and will be shown again when you switch back to `PLAY` mode.

- The tas command `Ghost_StartReplay` will force the game to load ghosts, if there's no ghost. However, if ghosts already exist, this command will not try to load new ghosts. (e.g. when you use a savestate and record a single room strat). In this case, you should use a `Ghost_Reload` tas command.

## Hotkeys

- You can use hotkeys to switch modes. It's configurable in the in-game menu.

## TasCommands

- A TasCommand is a command that only works in tas files.

- You can use `Ghost_StartRecord` to switch to RECORD mode in tas.

- You can use `Ghost_StopRecord` to stop recording.

Though it's easier to do that by just stopping the tas, assuming the record is started by a TasCommand.

- You can use `Ghost_StartReplay` to switch in `PLAY` mode in tas.

If ghosts already exist, this command will not try to find new ghosts.

- You can use `Ghost_Reload` to force the game to drop all ghosts and load ghosts again.

- You can use `Ghost_StopReplay` to stop replay.

- `Ghost_StartRecord` and `Ghost_StartReplay` should be before the first frame in the tas file. Or at least, they should be put in the same position of a tas file. Otherwise, you will find that the Ghost starts running earlier/later than it should be.

- When tas ends, the mode will return to `PLAY` or `OFF` mode (if it's changed by a TasCommand), depending on its original value. It will never return to `RECORD` mode.

- You don't have to remember their names, as you can see these commands in Studio with auto-complete, they all start with "Ghost_".

- See [TasCommandExport](Source/ModInterop/TasCommandExport.cs) for more information.

## ConsoleCommands

- You can use `ghost_off`, `ghost_record`, `ghost_play` to switch modes in console.

- Use `ghost_mark_level_end` to manually instruct that current level is completed. E.g. use it when you return to map after collecting cassettes (use it both when RECORD and PLAY).

- Use `ghost_forward [int frames]` to make GhostReplayer advance/delay some frames. Only works when `ForceSync` = false.

## Force Sync

- If `Force Sync` is ON, then the ghost will sync with you when you go to next room. The Ghost will be waiting for you if she is faster, or will catch up if she is slower.

## Ghost Info HUD and Ghost Custom Info

- You can drag and move it.

- Ghost info HUD = the TAS Info HUD when you record the ghosts. So if you closes TAS Info HUD when recording ghosts, then Ghost info HUD will be empty.

- Ghost custom info are calculated when you record the ghosts. So when you are playing the Ghost files, changing the Custom Info Template will not provide you more info.

- Ghost info HUD is disabled whenever TAS Info HUD is disabled, no matter what we've chosen in mod menu.

## Ghost Compare Time

- If you change the route (e.g. enter a room that the ghost doesn't enter, and vice versa), then the comparing time system will refuse to show the comparison.

## Other Features

- You can use `Ghost_SetName [ghost_name]` to name the ghost being recorded. Note that this command should be after level starts, but before the first room gets finished. If you doesn't use this command, then the ghost will use the default name (also customizable in mod settings).

- You can use `Ghost_LockComparer [ghost_name]` to force the comparer to always compare against that ghost.

## Issues

- The `Mode Switch` hotkey and CelesteTAS's `Lock Camera` hotkey have same default values (LeftControl + H).

- There may be some incompatibility with savestates. Please let me know.