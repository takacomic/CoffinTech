using CoffinTech;
using MelonLoader;

[assembly: MelonInfo(typeof(CoffinTechMod), ModInfo.Name, ModInfo.Version, ModInfo.Author, ModInfo.Download)]
[assembly: MelonGame("poncle", "Vampire Survivors")]

namespace CoffinTech;
internal static class ModInfo
{
    internal const string Name = "CoffinTech";
    internal const string Author = "Takacomic";
    internal const string Version = "1.0.0";
    internal const string Download = "https://github.com/takacomic/.../latest";
}

public class CoffinTechMod : MelonMod
{
    internal static bool BypassChecksum;
    private MelonPreferences_Category _category;
    private MelonPreferences_Entry<bool> _entry;
    public override void OnInitializeMelon()
    {
        _category = MelonPreferences.CreateCategory(nameof(CoffinTechMod), "CoffinTech");
        _entry = _category.CreateEntry("Bypass Save Checksum Check", true);
        
        BypassChecksum = _entry.Value;
    }
}