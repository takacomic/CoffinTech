using CoffinTech;
using CoffinTech.Utils;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(CoffinTechMod), ModInfo.Name, ModInfo.Version, ModInfo.Author, ModInfo.Download)]
[assembly: MelonGame("poncle", "Vampire Survivors")]
[assembly: MelonOptionalDependencies("SurvivorModMenu")]

namespace CoffinTech;
internal static class ModInfo
{
    internal const string Name = "CoffinTech";
    internal const string Author = "Takacomic";
    internal const string Version = "1.2.4";
    internal const string Download = "https://github.com/takacomic/.../latest";
}

public class CoffinTechMod : MelonMod
{
    internal static bool BypassChecksum;
    internal static MelonPreferences_Entry<bool> BypassChecksumEntry;
    internal static MelonPreferences_Entry<bool> DebugLoggingEntry;
    internal static MelonPreferences_Entry<bool> PowerUpEntry;
    internal static bool DebugLoggingEnabled;
    internal static bool PowerUpEnabled;
    internal static MelonPreferences_Entry<bool> UnityExplorerWarnEntry;
    internal static bool UnityExplorerWarnEnabled;
    
    public static bool OtherPluginPresent { get; private set; }
    public override void OnInitializeMelon()
    {
        MelonPreferences_Category category = MelonPreferences.CreateCategory(nameof(CoffinTechMod), "CoffinTech");
        BypassChecksumEntry = category.CreateEntry("Bypass Save Checksum Check", true);
        DebugLoggingEntry = category.CreateEntry("Enable Debug Logging", false);
        PowerUpEntry = category.CreateEntry("Enable Passive Level PowerUps", false);
        UnityExplorerWarnEntry = category.CreateEntry("Warn About UnityExplorer Hide On Startup", true);
        BypassChecksum = BypassChecksumEntry.Value;
        DebugLoggingEnabled = DebugLoggingEntry.Value;
        UnityExplorerWarnEnabled = UnityExplorerWarnEntry.Value;
        PowerUpEnabled = PowerUpEntry.Value;

        foreach (var mod in MelonLoader.MelonPlugin.RegisteredMelons)
        {
            MelonLogger.Msg($"Found mod: {mod.Info.Name}");
        }
        OtherPluginPresent = MelonLoader.MelonPlugin.RegisteredMelons
            .Any(m => m.Info.Name == "SurvivorModMenu");
        if (OtherPluginPresent)
        {
            ModSettings.Initialize();
        }
    }
}
