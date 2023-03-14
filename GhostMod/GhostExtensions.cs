using System.Reflection;

namespace Celeste.Mod.Ghost;

public static class GhostExtensions {
    private static readonly FieldInfo f_Player_wasDashB = typeof(Player).GetField("wasDashB", BindingFlags.NonPublic | BindingFlags.Instance);

    public static bool GetWasDashB(this Player self)
        => (bool) f_Player_wasDashB.GetValue(self);
}