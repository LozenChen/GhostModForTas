using System;

namespace Celeste.Mod.GhostModForTas.Utils;
internal static class TimeSpanFix {

    // taken from CelesteTAS
    public static long SecondsToTicks(this float seconds) {
        // .NET Framework rounded TimeSpan.FromSeconds to the nearest millisecond.
        // See: https://github.com/EverestAPI/Everest/blob/dev/NETCoreifier/Patches/TimeSpan.cs
        double millis = seconds * 1000 + (seconds >= 0 ? +0.5 : -0.5);
        return (long)millis * TimeSpan.TicksPerMillisecond;
        // this recovers NET 4.5.2 behavior
        // note that Everest has a TimeSpanShims class to patch TimeSpan class, but that only applies to non-core mods
        // so we have to do it on our own
    }

    public static TimeSpan Net8FromSeconds(this float seconds) {
        return TimeSpan.FromSeconds(seconds);
    }
}