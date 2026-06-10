using System.Reflection;
using HarmonyLib;
using Il2CppVampireSurvivors.Data;
using CoffinTech.Utils;
using Il2CppI2.Loc;
using Il2CppI2.Loc.SimpleJSON;
using Il2CppNewtonsoft.Json;
using Il2CppNewtonsoft.Json.Linq;
using Il2CppVampireSurvivors.App.Data;
using MelonLoader;

namespace CoffinTech.Patches;

[HarmonyPatch(typeof(DataManager))]
public class DataManagerPatches
{
    public static DataManager _instance;
    private static bool _patched;
    private static Newtonsoft.Json.Linq.JObject? _jObject;
    
    [HarmonyPatch(nameof(DataManager.LoadBaseJObjects))]
    [HarmonyPostfix]
    private static void Postfix(DataManager __instance)
    {
        _instance = __instance;
        if (!CoffinTechMod.PowerUpEnabled) return;

        EmbeddedResourseReader.ReadEmbeddedText(Assembly.GetExecutingAssembly(),
            "CoffinTech.resources.PowerUp_Passive.json", out var json);
        if (json is null)
        {
            MelonLogger.Msg("Failed to read embedded resource PowerUp_Passive.json.");
            return;
        }

        var jObject = JsonConvert.DeserializeObject<JObject>(json);
        _jObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(json);
        __instance._allPowerUpsJson = jObject;
    }
    
    [HarmonyPatch(nameof(DataManager.MergeInJsonData))]
    [HarmonyPostfix]
    private static void Postfix(DataManager __instance, DataManagerSettings settings, DlcType dlcType)
    {
        if (_patched) return;
        _patched = true;
        if (_jObject is null) return;
        var languageData = LocalizationManager.Sources._items.First();
        
        foreach (var kvp in _jObject)
        {
            var prefix = "powerUpLang/{" + kvp.Key + "}";
            foreach (var kvp2 in kvp.Value[0] as Newtonsoft.Json.Linq.JObject)
            {
                if (kvp2.Key == "description")
                {
                    var descLoc = languageData.GetTermData(prefix + "description");
                    descLoc.SetTranslation(0, kvp2.Value.ToString());
                }
            }
        }
    }
}