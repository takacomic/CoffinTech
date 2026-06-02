using Il2CppVampireSurvivors.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CoffinTech.Utils;

public class ModSecrets
{
    [JsonIgnore] private bool _inDLC = false;

    [JsonIgnore] private string _dlcKey = "";
    //SecretData
    [JsonProperty("description")] 
    private string _description = "";

    [JsonProperty("characterToUnlock")] 
    private CharacterType? _characterToUnlock;

    [JsonProperty("weaponToUnlock")] 
    private WeaponType? _weaponToUnlock;

    [JsonProperty("stageToUnlock")] 
    private StageType? _stageToUnlock;

    [JsonProperty("hyperToUnlock")] 
    private StageType? _hyperToUnlock;

    [JsonProperty("relicToUnlock")] 
    private ItemType? _relicToUnlock;

    [JsonProperty("arcanaToUnlock")] 
    private ArcanaType? _arcanaToUnlock;

    [JsonProperty("powerUpToUnlock")] 
    private PowerUpType? _powerUpToUnlock;

    [JsonProperty("mistery")] 
    private bool _mistery = true;

    [JsonProperty("achieved")] 
    private bool _achieved = false;

    [JsonProperty("isSpell")] 
    private bool? _isSpell;

    [JsonProperty("spell")] 
    private string? _spell;

    [JsonProperty("special")] 
    private string? _special;

    [JsonProperty("hidden")] 
    private bool? _hidden;

    [JsonProperty("goldPrize")] 
    private int? _goldPrize;

    [JsonProperty("isModifier")] 
    private bool? _isModifier;

    [JsonProperty("skinsToUnlock")] 
    private List<Skin>? _skinsToUnlock;

    [JsonProperty("weaponListToUnlock")] 
    private List<WeaponType>? _weaponListToUnlock;

    [JsonProperty("requiresRelic")] 
    private ItemType? _requiresRelic;

    [JsonProperty("customTexture")] 
    private string? _customTexture;

    [JsonProperty("customFrame")] 
    private string? _customFrame;

    [JsonProperty("customSmallTexture")] 
    private string? _customSmallTexture;

    [JsonProperty("customSmallFrame")] 
    private string? _customSmallFrame;
    
    public struct Skin
    {
        [JsonProperty("character")] 
        public CharacterType? CharacterType;

        [JsonProperty("skin")] 
        public SkinType? _skinType;

        [JsonProperty("weaponOnly")] 
        public bool? weaponOnly;
    }

    private JObject ToData()
    {
        var json = JsonConvert.SerializeObject(new JObject(this), Formatting.Indented, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        });
        return JObject.Parse(json);
    }

}