using System.Reflection;
using CoffinTech.Patches;
using CoffinTech.Utils;
using Il2CppI2.Loc;
using Il2CppNewtonsoft.Json;
using Il2CppNewtonsoft.Json.Linq;
using Il2CppVampireSurvivors.Data;
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
    
    internal static void SetPowerUp(bool value)
    {
        if (CoffinTechMod.PowerUpEntry == null)
        {
            return;
        }

        CoffinTechMod.PowerUpEntry.Value = value;
        CoffinTechMod.PowerUpEnabled = value;

        if (value)
        {
            EmbeddedResourseReader.ReadEmbeddedText(Assembly.GetExecutingAssembly(),
                "CoffinTech.resources.PowerUp_Passive.json", out var json);
            if (json is null)
            {
                MelonLogger.Msg("Failed to read embedded resource PowerUp_Passive.json.");
                return;
            }

            var jObject = JsonConvert.DeserializeObject<JObject>(json);
            DataManagerPatches._instance.AllPowerUps = jObject.ToObject<Il2CppSystem.Collections.Generic.Dictionary<PowerUpType, JArray>>();
            DataManagerPatches._instance._powerUpData = null;
            DataManagerPatches._instance.GetConvertedPowerUpData();
        }
        else
        {
            EmbeddedResourseReader.ReadEmbeddedText(Assembly.GetExecutingAssembly(),
                "CoffinTech.resources.PowerUp_Base.json", out var json);
            if (json is null)
            {
                MelonLogger.Msg("Failed to read embedded resource PowerUp_Base.json.");
                return;
            }

            var jObject = JsonConvert.DeserializeObject<JObject>(json);
            DataManagerPatches._instance.AllPowerUps = jObject.ToObject<Il2CppSystem.Collections.Generic.Dictionary<PowerUpType, JArray>>();
            DataManagerPatches._instance._powerUpData = null;
            DataManagerPatches._instance.GetConvertedPowerUpData();
        }
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

    internal static void DumpWeapons()
    {
        var json = new JObject();
        var types = new JArray();
        var names = new JArray();
        foreach (var kvp in DataManagerPatches._instance._weaponData)
        {
            
            json.Add(kvp.Key.ToString(), Defaults.LocalizationManager.GetTranslation(kvp.Value[0].GetLocalizedNameTerm(kvp.Key)).ToString());
            types.Add(kvp.Key.ToString());
            names.Add(Defaults.LocalizationManager.GetTranslation(kvp.Value[0].GetLocalizedNameTerm(kvp.Key)));
        }
        json.Add("types", types);
        json.Add("names", names);
        var directory = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "dumps");
        var path = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "dumps", "weapons.json");
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        //if (!File.Exists(path)) File.Create(path);
        File.WriteAllText(path, json.ToString());
    }

    internal static void DumpArcana()
    {
        var json = new JObject();
        var types = new JArray();
        var names = new JArray();
        foreach (var kvp in DataManagerPatches._instance.AllArcanas)
        {
            
            json.Add(kvp.Key.ToString(), Defaults.LocalizationManager.GetTranslation(kvp.Value.GetLocalizedNameTerm(kvp.Key)).ToString());
            types.Add(kvp.Key.ToString());
            names.Add(Defaults.LocalizationManager.GetTranslation(kvp.Value.GetLocalizedNameTerm(kvp.Key)));
        }
        json.Add("types", types);
        json.Add("names", names);
        var directory = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "dumps");
        var path = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "dumps", "arcana.json");
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        //if (!File.Exists(path)) File.Create(path);
        File.WriteAllText(path, json.ToString());
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
        builder.AddToggle("PowerUp Passives", () => CoffinTechMod.PowerUpEnabled, SetPowerUp);
        builder.AddButton("Dump WeaponType and Translations", DumpWeapons);
        builder.AddButton("Dump ArcanaType and Translations", DumpArcana);
    }

}
