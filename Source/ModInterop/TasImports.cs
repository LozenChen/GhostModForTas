using Microsoft.Xna.Framework;
using MonoMod.ModInterop;
using System;
using TAS.EverestInterop;

namespace Celeste.Mod.GhostModForTas.ModInterop;


internal static class TasImports {
    internal static Vector2 MousePosition => MouseInput.Position;

    internal static Vector2 PositionDelta => MouseInput.PositionDelta;

    internal static Vector2 LastPosition => MousePosition - PositionDelta;

    internal static float ZoomLevel => TAS.Gameplay.CenterCamera.ZoomLevel;

    internal static bool Manager_Running => __CelesteTasImports.IsTasActive();

    internal static void PopupMessageToStudio(string title, string text) {
        __CelesteTasImports.PopupMessageToStudio?.Invoke(title, text);
    }

    internal static void AbortTas(string message) {
        TAS.GlobalVariables.AbortTas(message);
    }

    [Initialize]
    private static void Initialize() {
        typeof(__CelesteTasImports).ModInterop();
    }
}

[ModImportName("CelesteTAS")]
internal static class __CelesteTasImports {
    public delegate void AddSettingsRestoreHandlerDelegate(EverestModule module, (Func<object> Backup, Action<object> Restore)? handler);
    public delegate void RemoveSettingsRestoreHandlerDelegate(EverestModule module);
    public delegate void DrawAccurateLineDelegate(Vector2 from, Vector2 to, Color color);
    public delegate void PopupMessageToStudioDelegate(string title, string text);

    /// Checks if a TAS is active (i.e. running / paused / etc.)
    public static Func<bool> IsTasActive = null!;

    /// Checks if a TAS is currently actively running (i.e. not paused)
    public static Func<bool> IsTasRunning = null!;

    /// Checks if the current TAS is being recorded with TAS Recorder
    public static Func<bool> IsTasRecording = null!;

    /// Registers custom delegates for backing up and restoring mod setting before / after running a TAS
    /// A `null` handler causes the settings to not be backed up and later restored
    public static AddSettingsRestoreHandlerDelegate AddSettingsRestoreHandler = null!;

    /// De-registers a previously registered handler for the module
    public static RemoveSettingsRestoreHandlerDelegate RemoveSettingsRestoreHandler = null!;

    public static PopupMessageToStudioDelegate PopupMessageToStudio = null!;

}