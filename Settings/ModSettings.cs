using MelonLoader;
using SurvivorModMenu.ModMenu;

namespace CoffinTech;

internal static class ModSettings
{
    

    internal static void Initialize()
    {
        Logger.DebugLogger.Msg("ModSettings.Initialize: registering mod menu.");
        ModMenuRegistry.Register("CoffinTech", "CoffinTech", BuildCoffinTechOptions);

    }

    internal static void SetBypassChecksum(bool value)
    {
        if (CoffinTechMod.BypassChecksumEntry == null)
        {
            return;
        }

        CoffinTechMod.BypassChecksumEntry.Value = value;
        CoffinTechMod.BypassChecksum = value;
        MelonPreferences.Save();
    }

    internal static void SetDebugLogging(bool value)
    {
        if (CoffinTechMod.DebugLoggingEntry == null)
        {
            return;
        }

        CoffinTechMod.DebugLoggingEntry.Value = value;
        CoffinTechMod.DebugLoggingEnabled = value;
        MelonPreferences.Save();
    }

    internal static void DisableUnityExplorerWarn()
    {
        if (CoffinTechMod.UnityExplorerWarnEntry == null)
        {
            return;
        }

        CoffinTechMod.UnityExplorerWarnEntry.Value = false;
        CoffinTechMod.UnityExplorerWarnEnabled = false;
        MelonPreferences.Save();
    }

    private static void BuildCoffinTechOptions(ModMenuBuilder builder)
    {
        if (builder == null)
        {
            return;
        }

        builder.AddToggle("Bypass Save Checksum", () => CoffinTechMod.BypassChecksum,
            SetBypassChecksum);
        builder.AddToggle("Debug Logging", () => CoffinTechMod.DebugLoggingEnabled, SetDebugLogging);
    }

}
