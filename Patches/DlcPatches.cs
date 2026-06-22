using CoffinTech.SaveData;
using HarmonyLib;
using Il2CppI2.Loc;
using Il2CppVampireSurvivors.App.Data;
using Il2CppVampireSurvivors.Data;
using Il2CppVampireSurvivors.Framework.DLC;
using Il2CppVampireSurvivors.Framework.Loading;
using Il2CppVampireSurvivors.Objects;
using MelonLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CoffinTech.Patches;

// Credits to Mercy(Salmacis32 -- MasqueradeOfLimerence)
// Stolen for Custom Merchant stuffs
// Wouldn't recommend using this
[HarmonyPatch(typeof(DlcLoader))]
public static class DlcPatches
{
    internal static Dictionary<DlcType, List<object>> _dlcTypes = new ();
    private static LanguageSourceData LanguageData;
    
    [HarmonyPatch(nameof(DlcLoader.LoadDlc))]
    [HarmonyPrefix]
    public static bool PreLoadDlc(DlcLoader __instance, DlcType dlcType, Action<BundleManifestData> onComplete)
    {
        foreach (var dlc in _dlcTypes)
        {
            if (dlcType != dlc.Key) continue;
            DlcLoader.ResetLoader();
            var dlcNullable = new Il2CppSystem.Nullable<DlcType>(dlc.Key)
            {
                value = dlcType
            };
            DlcLoader._dlcType = dlcNullable;
            DlcLoader._onComplete = onComplete;
            DlcLoader.UpdateProgress();

            Action<BundleManifestData> action = bmd =>
            {
                DlcLoader._manifestState = ((!DlcLoader.DidTaskError(DlcLoader._manifestState))
                    ? DlcLoadState.Complete
                    : DlcLoadState.Error);
                DlcLoader._manifest = bmd;
                DlcLoader._locationsState = DlcLoadState.Complete;
                DlcLoader._spritesState = DlcLoadState.Complete;
                DlcLoader.UpdateProgress();
            };

            ManifestLoader.LoadManifest(CreateBundle(dlc.Value[0] as string,dlc.Value[1] as string, dlc.Value[2] as DataManagerSettings), dlc.Key, action);
            LanguageData = null;
            return false;
        }
        
        return true;
    }
    
    
    
    public static void AddDlc(DlcType dlcType, List<object> bundleManifestData)
    {
        _dlcTypes.Add(dlcType, bundleManifestData);
    }
    
    private static BundleManifestData CreateBundle(string name, string version, DataManagerSettings data)
    {
        LanguageData = LocalizationManager.Sources._items.First();
        if (data._ItemDataJsonAsset) data._ItemDataJsonAsset = Items(JsonConvert.DeserializeObject<JObject>(data._ItemDataJsonAsset.text ?? "{}"));
        if (data._CharacterDataJsonAsset) data._CharacterDataJsonAsset = Characters(JsonConvert.DeserializeObject<JObject>(data._CharacterDataJsonAsset.text ?? "{}"));
        if (data._SecretsDataJsonAsset) data._SecretsDataJsonAsset = Secrets(JsonConvert.DeserializeObject<JObject>(data._SecretsDataJsonAsset.text ?? "{}"));
        
        var modDlcData = ScriptableObject.CreateInstance<BundleManifestData>();
        modDlcData._Version = version; 
        modDlcData.name = name;
        modDlcData._DataFiles = data;
        
        return modDlcData;
    }
    
    private static TextAsset Characters(JObject data)
    {
        var obj = CharacterNameToType(data);
        CharacterLanguage(obj);
        return new TextAsset(JsonConvert.SerializeObject(obj));
    }
    
    private static JObject CharacterNameToType(JObject data)
    {
        JObject jObject2 = new();
        // TODO: Add ??? Support
        foreach (var kvp in data)
        {
            JArray jArray = new();
            foreach (var obj in kvp.Value as JArray)
            {
                JObject jObject3 = new();
                foreach (var kvp2 in obj as JObject)
                {
                    if (kvp2.Key == "requiresRelic")
                    {
                        jObject3.Add(kvp2.Key, ModOptionsData.CustomItem(kvp2.Value.ToString()).Value.ToString());
                    }
                    else
                    {
                        jObject3.Add(kvp2.Key, kvp2.Value);
                    }
                }
                jArray.Add(jObject3);
            }
            var character = ModOptionsData.CustomCharacter(kvp.Key);
            jObject2.Add(character.Value.ToString(), jArray);
        }
        return jObject2;
    }
    
    private static void CharacterLanguage(JObject data)
    {
        foreach (var kvp in data)
        {
            var prefix = "characterLang/{" + kvp.Key + "}";
            foreach (var kvp2 in kvp.Value[0] as JObject)
            {
                if (kvp2.Key == "description")
                {
                    var descLoc = LanguageData.AddTerm(prefix + "description");
                    descLoc.SetTranslation(0, kvp2.Value.ToString());
                }
                if (kvp2.Key == "charName")
                {
                    var nameLoc = LanguageData.AddTerm(prefix + "charName");
                    nameLoc.SetTranslation(0, kvp2.Value.ToString());
                }
                if (kvp2.Key == "surName")
                {
                    var nameLoc = LanguageData.AddTerm(prefix + "surName");
                    nameLoc.SetTranslation(0, kvp2.Value.ToString());
                }
                if (kvp2.Key == "prefix")
                {
                    var nameLoc = LanguageData.AddTerm(prefix + "prefix");
                    nameLoc.SetTranslation(0, kvp2.Value.ToString());
                }
            }
            
        }
    }

    private static TextAsset Items(JObject data)
    {
        var obj = ItemNameToType(data);
        ItemsLanguage(obj);
        return new TextAsset(JsonConvert.SerializeObject(obj));
    }

    private static JObject ItemNameToType(JObject data)
    {
        JObject jObject2 = new();
        // TODO: Add Requires Support
        foreach (var kvp in data)
        {
            var isRelic = false;
            foreach (var kvp2 in kvp.Value as JObject)
            {
                if (kvp2.Key == "isRelic")
                {
                    isRelic = kvp2.Value.ToObject<bool>();
                }
            }
            var item = ModOptionsData.CustomItem(kvp.Key, isRelic);
            jObject2.Add(item.Value.ToString(), kvp.Value);
        }
        
        return jObject2;
    }
    
    private static void ItemsLanguage(JObject data)
    {
        foreach (var kvp in data)
        {
            var prefix = "itemLang/{" + kvp.Key + "}";
            foreach (var kvp2 in kvp.Value as JObject)
            {
                if (kvp2.Key == "description")
                {
                    var descLoc = LanguageData.AddTerm(prefix + "description");
                    descLoc.SetTranslation(0, kvp2.Value.ToString());
                }
                if (kvp2.Key == "name")
                {
                    var nameLoc = LanguageData.AddTerm(prefix + "name");
                    nameLoc.SetTranslation(0, kvp2.Value.ToString());
                }
            }
            
        }
    }

    private static TextAsset Secrets(JObject data)
    {
        // TODO have SecretsName to out a jobject for secretslanguage and then convert it to a textasset
        var obj = SecretsNameToType(data);
        SecretsLanguage(obj);
        return new TextAsset(JsonConvert.SerializeObject(obj));
    }
    
    private static void SecretsLanguage(JObject data)
    {
        foreach (var kvp in data)
        {
            var prefix = "secretLang/{" + kvp.Key + "}";
            foreach (var kvp2 in kvp.Value as JObject)
            {
                if (kvp2.Key != "description") continue;
                var descLoc = LanguageData.AddTerm(prefix + "description");
                descLoc.SetTranslation(0, kvp2.Value.ToString());
            }
            
        }
    }

    private static JObject SecretsNameToType(JObject data)
    {
        JObject jObject2 = new();
        // TODO: Add move ToUnlock Support
        foreach (var kvp in data)
        {
            JObject jObject3 = new();
            foreach (var kvp2 in kvp.Value as JObject)
            {
                if (kvp2.Key == "relicToUnlock")
                {
                    jObject3.Add(kvp2.Key, ModOptionsData.CustomItem(kvp2.Value.ToString()).Value.ToString());
                }
                else if (kvp2.Key == "requiresRelic")
                {
                    jObject3.Add(kvp2.Key, ModOptionsData.CustomItem(kvp2.Value.ToString()).Value.ToString());
                }
                else if (kvp2.Key == "characterToUnlock")
                {
                    jObject3.Add(kvp2.Key, ModOptionsData.CustomCharacter(kvp2.Value.ToString()).Value.ToString());
                }
                else if (kvp2.Key == "skinsToUnlock")
                {
                    var jArray = new JArray();
                    var jObject4 = new JObject();
                    foreach (var skin in kvp2.Value as JArray)
                    {
                        foreach (var skin2 in skin as JObject)
                        {
                            if (skin2.Key == "character")
                            {
                                jObject4.Add(skin2.Key,
                                    ModOptionsData.CustomCharacter(skin2.Value.ToString()).Value.ToString());
                            }
                            else
                            {
                                jObject4.Add(skin2.Key, skin2.Value.ToString());
                            }
                        }
                        jArray.Add(jObject4);
                        jObject4 = new JObject();
                    }
                    jObject3.Add(kvp2.Key, jArray);
                }
                else
                {
                    jObject3.Add(kvp2.Key, kvp2.Value);
                }
            }
            var secret = ModOptionsData.CustomSecret(kvp.Key);
            jObject2.Add(secret.Value.ToString(), jObject3);
        }
        
        return jObject2;
    }
    
    
}

[HarmonyPatch(typeof(LicenseManager))]
public static class License
{
    [HarmonyPatch(nameof(LicenseManager.SortDlcLists))]
    [HarmonyPostfix]
    public static void PostSortDlcLists(LicenseManager __instance)
    {
        foreach (var dlc in DlcPatches._dlcTypes.Keys)
        {
            __instance.IncludedDlc.Add(dlc);
        }
    }
}