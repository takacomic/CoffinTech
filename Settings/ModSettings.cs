using CoffinTech.Logger;
using Il2CppTMPro;
using MelonLoader;
using SurvivorModMenu;
using UnityEngine;
using UnityEngine.UI;

namespace CoffinTech;

internal static class ModSettings
{
    internal static MelonPreferences_Entry<bool> BypassChecksumEntry;
    internal static MelonPreferences_Entry<bool> DebugLoggingEntry;
    internal static bool DebugLoggingEnabled;
    internal static MelonPreferences_Entry<bool> UnityExplorerWarnEntry;
    internal static bool UnityExplorerWarnEnabled;

    internal static void Initialize()
    {
        MelonPreferences_Category category = MelonPreferences.CreateCategory(nameof(CoffinTechMod), "CoffinTech");
        BypassChecksumEntry = category.CreateEntry("Bypass Save Checksum Check", true);
        DebugLoggingEntry = category.CreateEntry("Enable Debug Logging", true);
        UnityExplorerWarnEntry = category.CreateEntry("Warn About UnityExplorer Hide On Startup", true);
        CoffinTechMod.BypassChecksum = BypassChecksumEntry.Value;
        DebugLoggingEnabled = DebugLoggingEntry.Value;
        UnityExplorerWarnEnabled = UnityExplorerWarnEntry.Value;
        Logger.DebugLogger.Msg("ModSettings.Initialize: registering mod menu.");

        ModMenuRegistry.Register("CoffinTech", "CoffinTech", BuildCoffinTechOptions);
    }

    internal static void SetBypassChecksum(bool value)
    {
        if (BypassChecksumEntry == null)
        {
            return;
        }

        BypassChecksumEntry.Value = value;
        CoffinTechMod.BypassChecksum = value;
        MelonPreferences.Save();
    }

    internal static void SetDebugLogging(bool value)
    {
        if (DebugLoggingEntry == null)
        {
            return;
        }

        DebugLoggingEntry.Value = value;
        DebugLoggingEnabled = value;
        MelonPreferences.Save();
    }

    internal static void DisableUnityExplorerWarn()
    {
        if (UnityExplorerWarnEntry == null)
        {
            return;
        }

        UnityExplorerWarnEntry.Value = false;
        UnityExplorerWarnEnabled = false;
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
        builder.AddToggle("Debug Logging", () => DebugLoggingEnabled, SetDebugLogging);
    }

}
