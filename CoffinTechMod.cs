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
    public override void OnInitializeMelon()
    {
        ModSettings.Initialize();
        
    }
}
