using System.Reflection;

namespace Celeste.Mod.Ghost {
public static class GhostExtensions {
    private readonly static FieldInfo f_Player_wasDashB = typeof(Player).GetField("wasDashB", BindingFlags.NonPublic | BindingFlags.Instance);

    public static bool GetWasDashB(this Player self)
        => (bool) f_Player_wasDashB.GetValue(self);
}
}