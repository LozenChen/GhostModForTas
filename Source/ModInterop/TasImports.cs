using Microsoft.Xna.Framework;
using TAS.EverestInterop;

namespace Celeste.Mod.GhostModForTas.ModInterop;

internal static class TasImports {
    internal static Vector2 MousePosition => MouseInput.Position;

    internal static Vector2 PositionDelta => MouseInput.PositionDelta;

    internal static Vector2 LastPosition => MousePosition - PositionDelta;

    internal static float ZoomLevel => TAS.Gameplay.CenterCamera.ZoomLevel;
}