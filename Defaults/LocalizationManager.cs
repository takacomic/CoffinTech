global using CoffinTech.Defaults;
//using Il2CppInterop.Runtime.Structs;

namespace CoffinTech.Defaults;

public class LocalizationManager
{
    private static readonly Il2CppI2.Loc.LanguageSourceData First = Il2CppI2.Loc.LocalizationManager.Sources._items.First();
    
    public static Il2CppI2.Loc.LanguageSourceData _First => First;
    
    public static Il2CppSystem.String GetTranslation(string key)
    {
        return Il2CppI2.Loc.LocalizationManager.GetTranslation(key, true, 0, true, false, null, null, true);
    }
    
    public static Il2CppSystem.String GetTranslation(Il2CppSystem.String key)
    {
        return Il2CppI2.Loc.LocalizationManager.GetTranslation(key, true, 0, true, false, null, null, true);
    }
}