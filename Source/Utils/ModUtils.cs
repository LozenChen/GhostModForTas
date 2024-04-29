using Celeste.Mod.Helpers;
using System;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.GhostModForTas.Utils;

// completely taken from Celeste TAS
internal static class ModUtils {
    public static readonly Assembly VanillaAssembly = typeof(Player).Assembly;

    public static Type GetType(string modName, string name, bool throwOnError = false, bool ignoreCase = false) {
        // we need mod name = mod metadata name here, where e.g. assembly name = FrostTempleHelper, mod metadata name = FrostHelper
        return GetAssembly(modName)?.GetType(name, throwOnError, ignoreCase);
    }
    // check here if you dont know what's the correct name for a nested type / generic type
    // https://learn.microsoft.com/zh-cn/dotnet/framework/reflection-and-codedom/specifying-fully-qualified-type-names

    public static Type GetType(string name, bool throwOnError = false, bool ignoreCase = false) {
        return FakeAssembly.GetFakeEntryAssembly().GetType(name, throwOnError, ignoreCase);
    }

    public static Type[] GetTypes() {
        return FakeAssembly.GetFakeEntryAssembly().GetTypes();
    }

    public static EverestModule GetModule(string modName) {
        return Everest.Modules.FirstOrDefault(module => module.Metadata?.Name == modName);
    }

    public static bool IsInstalled(string modName) {
        return GetModule(modName) != null;
    }

    public static Assembly GetAssembly(string modName) {
        return GetModule(modName)?.GetType().Assembly;
    }
}

