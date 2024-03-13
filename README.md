# GhostModForTas

### License: MIT

----

This Celeste mod is base on DemoJameson's [GhostMod](https://github.com/DemoJameson/GhostMod), which is a fork of Everest Team's [GhostMod](https://github.com/EverestAPI/GhostMod)

# How to use

Step1: Switch to RECORD mode manually, or use a StartGhostRecording command before the first frame in the tas file.

Step2: RESTART chapter or use a CONSOLE LOAD command to start your run to record. If you succeed, you will find a Recording icon in the bottom-right of the screen.

Step3: Whenever you finishes a room, a Ghost file will be written to storage.

Step4: Wait until your tas finishes, or use a StopGhostRecording command, to stop recording. Note that if the last recording room is not finished, then it will be dropped.

Step5: Switch to PLAY mode manually, and enter the same level, to play your records.

# Force Sync

- If Force Sync is ON, then the ghost will sync with you when you go to next room. The Ghost will be waiting for you if she is faster, or will catch up if she is slower.

# Ghost Info HUD and Ghost Custom Info

- You can drag and move it.

- Ghost info HUD = the TAS Info HUD when you record the ghosts. So if you closes TAS Info HUD when recording ghosts, then Ghost info HUD will be empty.

- Ghost custom info are calculated when you record the ghosts. So when you are playing the Ghost files, changing the Custom Info Template will not provide you more info.

# Ghost Compare Time

- If you change the route (e.g. enter a room that the ghost doesn't enter, and vice versa), then the comparing time system will refuse to show the comparison.

# Hotkey

- You can use hotkeys to switch modes.