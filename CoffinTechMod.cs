using CoffinTech;
using MelonLoader;

[assembly: MelonInfo(typeof(CoffinTechMod), ModInfo.Name, ModInfo.Version, ModInfo.Author, ModInfo.Download)]
[assembly: MelonGame("poncle", "Vampire Survivors")]
[assembly: MelonOptionalDependencies("SurvivorModMenu")]

namespace CoffinTech;
internal static class ModInfo
{
    internal const string Name = "CoffinTech";
    internal const string Author = "Takacomic";
    internal const string Version = "1.1.0";
    internal const string Download = "https://github.com/takacomic/.../latest";
}

public class CoffinTechMod : MelonMod
{
    internal static bool BypassChecksum;
    internal static MelonPreferences_Entry<bool> BypassChecksumEntry;
    internal static MelonPreferences_Entry<bool> DebugLoggingEntry;
    internal static bool DebugLoggingEnabled;
    internal static MelonPreferences_Entry<bool> UnityExplorerWarnEntry;
    internal static bool UnityExplorerWarnEnabled;
    
    public static bool OtherPluginPresent { get; private set; }
    public override void OnInitializeMelon()
    {
        MelonPreferences_Category category = MelonPreferences.CreateCategory(nameof(CoffinTechMod), "CoffinTech");
        BypassChecksumEntry = category.CreateEntry("Bypass Save Checksum Check", true);
        DebugLoggingEntry = category.CreateEntry("Enable Debug Logging", true);
        UnityExplorerWarnEntry = category.CreateEntry("Warn About UnityExplorer Hide On Startup", true);
        BypassChecksum = BypassChecksumEntry.Value;
        DebugLoggingEnabled = DebugLoggingEntry.Value;
        UnityExplorerWarnEnabled = UnityExplorerWarnEntry.Value;
        
        
        OtherPluginPresent = RegisteredMelons
            .Any(m => m.Info.Name == "SurvivorModMenu");
        if (OtherPluginPresent)
        {
            ModSettings.Initialize();
        }
    }
}
