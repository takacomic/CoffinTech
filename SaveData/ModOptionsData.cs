using System.Reflection;
using CoffinTech.Extensions;
using CoffinTech.Logger;
using CoffinTech.Patches;
using Il2CppVampireSurvivors.Data;
using Il2CppVampireSurvivors.Data.Props;
using Newtonsoft.Json.Linq;

namespace CoffinTech.SaveData;

public class ModOptionsData
{
    // Constants
    private const int Version = 1;
    private const string ModSaveDataPath = "UserData/ModSaveData";
    private const string UnclaimedDataFileName = "UnclaimedCharacterData.json";
    private const CharacterType DefaultCharacter = CharacterType.ANTONIO;
    
    // Static data storage
    private static readonly Dictionary<CharacterType, string> CustomCharacterNames = new();
    private static readonly Dictionary<string, CharacterType> CustomCharacterIDs = new();
    private static readonly List<string> UnclaimedCustomCharacterIDs = new();
    
    private static readonly List<string> BoughtCharacters = new();
    private static readonly JObject CharacterEggCount = new();
    private static readonly JObject CharacterEggInfo = new();
    private static readonly JObject CharacterEnemiesKilled = new();
    private static readonly JObject CharacterStageData = new();
    private static readonly JObject CharacterSurvivedMinutes = new();
    private static readonly List<string> OpenedCoffins = new();
    private static readonly JObject SelectedSkins = new();
    private static readonly JObject SelectedSkinsV2 = new();
    private static readonly JObject StageCompletionLog = new();
    private static readonly List<string> UnlockedCharacters = new();
    private static readonly JObject UnlockedSkins = new();
    private static readonly JObject UnlockedSkinsV2 = new();

    // Unclaimed data storage
    private static readonly JObject UnclaimedData = new();
    private static bool _unclaimedDataLoaded;

    // Instance fields
    private PlayerOptionsData _staticPod = null!;
    private PlayerOptionsData _cleansedPod = null!;
    private PlayerOptionsData _writtenPod = null!;
    
    private readonly string[] _doNotCopy = {
        "ObjectClass", "Pointer", "WasCollected",
        "BoughtCharacters", "CharacterEggCount", "CharacterEggInfo", "CharacterEnemiesKilled",
        "CharacterStageData", "CharacterSurvivedMinutes", "OpenedCoffins", "SelectedCharacter", "SelectedSkins", "SelectedSkinsV2", "StageCompletionLog",
        "UnlockedCharacters", "UnlockedSkins", "UnlockedSkinsV2"
    };

    // Properties
    internal static JObject ObjectToWrite { get; } = new();

    // Public API
    public static void SetCharacterId(CharacterType character, string id)
    {
        CustomCharacterNames[character] = id;
        CustomCharacterIDs[id] = character;
    }

    // Public Instance Methods
    internal PlayerOptionsData ModDataRemover(PlayerOptionsData pod)
    {
        _staticPod = pod;
        _cleansedPod = DefaultPod(pod);
        
        ObjectToWrite.TryAdd("version", Version);
        
        if (!CustomCharacterNames.TryGetValue(_staticPod.SelectedCharacter, out var value) && 
            !Enum.IsDefined(typeof(CharacterType), _staticPod.SelectedCharacter))
            _staticPod.SelectedCharacter = DefaultCharacter;
            
        _cleansedPod.SelectedCharacter = _staticPod.SelectedCharacter;
        ObjectToWrite["selectedCharacter"] = value ?? nameof(DefaultCharacter);
        
        EnsureModSaveDataDirectory();
        
        // Process all character data
        ProcessAllRemoverMethods();
        
        return _cleansedPod;
    }

    internal PlayerOptionsData ModDataSetter(PlayerOptionsData pod)
    {
        _writtenPod = pod;
        
        // Load unclaimed data once per game session
        if (!_unclaimedDataLoaded)
        {
            LoadUnclaimedData();
            _unclaimedDataLoaded = true;
        }
        else
        {
            try
            {
                File.Delete(GetUnclaimedDataPath());
            }
            catch (Exception ex)
            {
                DebugLogger.Msg($"ModOptionsData::ModDataSetter: Failed to delete file '{GetUnclaimedDataPath()}': {ex.Message}");
            }
            
        }
        
        JObject jObject = SteamworksCloudStoragePatch.ObjectToRead;
        if(!jObject.HasValues) return pod;
        _writtenPod.SelectedCharacter = (CustomCharacterIDs).GetValueOrDefault(
            jObject["selectedCharacter"]?.Value<string>(), DefaultCharacter);
            
        ProcessAllSetterMethods(jObject);
        
        // Process unclaimed data if it exists
        if (UnclaimedData.Count > 0)
        {
            ProcessAllSetterMethods(UnclaimedData);
        }
        
        // Save any new unclaimed data
        if (UnclaimedCustomCharacterIDs.Count > 0)
        {
            SaveUnclaimedData();
        }
        
        return _writtenPod;
    }

    // Private Helper Methods
    private static bool IsCustomCharacter(CharacterType character)
    {
        return CustomCharacterNames.ContainsKey(character);
    }

    private static bool IsUnclaimedCharacter(string characterId)
    {
        return !CustomCharacterIDs.ContainsKey(characterId) && 
               !Enum.TryParse<CharacterType>(characterId, out _);
    }

    private static void EnsureModSaveDataDirectory()
    {
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), ModSaveDataPath);
        if (!Directory.Exists(fullPath))
            Directory.CreateDirectory(fullPath);
    }

    private static string GetUnclaimedDataPath()
    {
        return Path.Combine(Directory.GetCurrentDirectory(), ModSaveDataPath, UnclaimedDataFileName);
    }

    private static void LoadUnclaimedData()
    {
        string filePath = GetUnclaimedDataPath();
        
        if (!File.Exists(filePath))
            return;
            
        try
        {
            string json = File.ReadAllText(filePath);
            JObject loadedData = JObject.Parse(json);
            
            // Merge loaded data into UnclaimedData
            foreach (var property in loadedData.Properties())
            {
                UnclaimedData[property.Name] = property.Value;
            }
        }
        catch (Exception ex)
        {
            MelonLoader.MelonLogger.Error($"Failed to load unclaimed character data: {ex.Message}");
        }
    }

    private static void SaveUnclaimedData()
    {
        string filePath = GetUnclaimedDataPath();
        
        try
        {
            JObject dataToSave = new JObject
            {
                ["version"] = Version,
                ["unclaimedCharacterIds"] = JArray.FromObject(UnclaimedCustomCharacterIDs)
            };
            
            // Add all unclaimed character data
            if (UnclaimedData.TryGetValue("boughtCharacters", out var boughtChars))
                dataToSave["boughtCharacters"] = boughtChars;
            if (UnclaimedData.TryGetValue("characterEggCount", out var eggCount))
                dataToSave["characterEggCount"] = eggCount;
            if (UnclaimedData.TryGetValue("characterEggInfo", out var eggInfo))
                dataToSave["characterEggInfo"] = eggInfo;
            if (UnclaimedData.TryGetValue("characterEnemiesKilled", out var enemiesKilled))
                dataToSave["characterEnemiesKilled"] = enemiesKilled;
            if (UnclaimedData.TryGetValue("characterStageData", out var stageData))
                dataToSave["characterStageData"] = stageData;
            if (UnclaimedData.TryGetValue("characterSurvivedMinutes", out var survivedMinutes))
                dataToSave["characterSurvivedMinutes"] = survivedMinutes;
            if (UnclaimedData.TryGetValue("openedCoffins", out var coffins))
                dataToSave["openedCoffins"] = coffins;
            if (UnclaimedData.TryGetValue("selectedSkins", out var skins))
                dataToSave["selectedSkins"] = skins;
            if (UnclaimedData.TryGetValue("selectedSkinsV2", out var skinsV2))
                dataToSave["selectedSkinsV2"] = skinsV2;
            if (UnclaimedData.TryGetValue("stageCompletionLog", out var completionLog))
                dataToSave["stageCompletionLog"] = completionLog;
            if (UnclaimedData.TryGetValue("unlockedCharacters", out var unlockedChars))
                dataToSave["unlockedCharacters"] = unlockedChars;
            if (UnclaimedData.TryGetValue("unlockedSkins", out var unlockedSkins))
                dataToSave["unlockedSkins"] = unlockedSkins;
            if (UnclaimedData.TryGetValue("unlockedSkinsV2", out var unlockedSkinsV2))
                dataToSave["unlockedSkinsV2"] = unlockedSkinsV2;
            
            File.WriteAllText(filePath, dataToSave.ToString(Newtonsoft.Json.Formatting.Indented));
        }
        catch (Exception ex)
        {
            MelonLoader.MelonLogger.Error($"Failed to save unclaimed character data: {ex.Message}");
        }
    }

    private void AddToUnclaimedData(string characterId, string dataKey, JToken value)
    {
        if (!UnclaimedCustomCharacterIDs.Contains(characterId))
        {
            UnclaimedCustomCharacterIDs.Add(characterId);
        }
        
        if (!UnclaimedData.ContainsKey(dataKey))
        {
            UnclaimedData[dataKey] = new JObject();
        }
        
        if (UnclaimedData[dataKey] is JObject jobject)
        {
            jobject[characterId] = value;
        }
        else if (UnclaimedData[dataKey] is JArray jarray && !jarray.Contains(characterId))
        {
            jarray.Add(characterId);
        }
    }

    private T ParseEnumWithFallback<T>(string value, T fallback) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, out var result))
            return result;
        
        if (int.TryParse(value, out var intValue) && Enum.IsDefined(typeof(T), intValue))
            return (T)(object)intValue;
            
        return fallback;
    }
    // Data Processing Helper Methods
    private void ProcessIl2CppCharacterList<T>(Il2CppSystem.Collections.Generic.List<T> source, 
        Il2CppSystem.Collections.Generic.List<T> target, List<string> customTarget, 
        Func<T, CharacterType> getCharacterType, Func<T, string> getCustomId)
    {
        foreach (var item in source)
        {
            var characterType = getCharacterType(item);
            if (!IsCustomCharacter(characterType))
            {
                target.Add(item);
                continue;
            }
            var customId = getCustomId(item);
            if (!customTarget.Contains(customId))
                customTarget.Add(customId);
        }
    }

    private void ProcessIl2CppCharacterDictionary<T>(Il2CppSystem.Collections.Generic.Dictionary<CharacterType, T> source, 
        Il2CppSystem.Collections.Generic.Dictionary<CharacterType, T> target, JObject customTarget, string objectToWriteKey)
    {
        foreach (Il2CppSystem.Collections.Generic.KeyValuePair<CharacterType, T> kvp in source)
        {
            var character = kvp.Key;
            if (!IsCustomCharacter(character))
            {
                target[character] = kvp.Value;
                continue;
            }
            var customId = CustomCharacterNames[character];
            if (kvp.Value != null)
                customTarget[customId] = JToken.FromObject(kvp.Value);
        }
        ObjectToWrite[objectToWriteKey] = customTarget;
    }

    private void ProcessCharacterDictionaryWithConversion<T, TU>(Il2CppSystem.Collections.Generic.Dictionary<CharacterType, T> source, 
        Il2CppSystem.Collections.Generic.Dictionary<CharacterType, T> target, 
        JObject customTarget, string objectToWriteKey, Func<T, TU> convertValue)
    {
        foreach (Il2CppSystem.Collections.Generic.KeyValuePair<CharacterType, T> kvp in source)
        {
            var character = kvp.Key;
            if (!IsCustomCharacter(character))
            {
                target[character] = kvp.Value;
                continue;
            }
            var customId = CustomCharacterNames[character];
            var convertedValue = convertValue(kvp.Value);
            if (convertedValue != null)
                customTarget[customId] = JToken.FromObject(convertedValue);
        }
        ObjectToWrite[objectToWriteKey] = customTarget;
    }

    private void ProcessCharacterListSetter(JArray jArray, Il2CppSystem.Collections.Generic.List<CharacterType> target, string dataKey)
    {
        foreach (string character in jArray)
        {
            if (character == null) continue;
            
            if (CustomCharacterIDs.TryGetValue(character, out var key))
            {
                target.Add(key);
            }
            else if (IsUnclaimedCharacter(character))
            {
                // Initialize array if needed
                if (!UnclaimedData.ContainsKey(dataKey))
                {
                    UnclaimedData[dataKey] = new JArray();
                }
                
                AddToUnclaimedData(character, dataKey, character);
            }
        }
    }

    private void ProcessCharacterDictionarySetter<T>(JObject jObject, Il2CppSystem.Collections.Generic.Dictionary<CharacterType, T> target, 
        Func<JToken, T> converter, T defaultValue, string dataKey)
    {
        foreach (KeyValuePair<string, JToken> kvp in jObject)
        {
            if (CustomCharacterIDs.TryGetValue(kvp.Key, out var key))
            {
                var value = converter(kvp.Value) ?? defaultValue;
                target[key] = value;
            }
            else if (IsUnclaimedCharacter(kvp.Key))
            {
                AddToUnclaimedData(kvp.Key, dataKey, kvp.Value ?? JValue.CreateNull());
            }
        }
    }

    private void ProcessCharacterListWithEnumSetter<T>(JObject jObject, Il2CppSystem.Collections.Generic.Dictionary<CharacterType, Il2CppSystem.Collections.Generic.List<T>> target, 
        Func<JToken, T> converter, string dataKey) where T : struct, Enum
    {
        foreach (KeyValuePair<string, JToken> kvp in jObject)
        {
            if (CustomCharacterIDs.TryGetValue(kvp.Key, out var key))
            {
                var dataList = new Il2CppSystem.Collections.Generic.List<T>();
                foreach (JToken token in kvp.Value as JArray ?? new JArray())
                {
                    if (ParseEnumWithFallback<T>(token.Value<string>(), default) is var value && !Equals(value, default(T)))
                        dataList.Add(value);
                }
                target[key] = dataList;
            }
            else if (IsUnclaimedCharacter(kvp.Key))
            {
                AddToUnclaimedData(kvp.Key, dataKey, kvp.Value ?? new JArray());
            }
        }
    }

    // Remover Methods
    private void ProcessAllRemoverMethods()
    {
        BoughtCharactersRemover();
        CharacterEggCountRemover();
        CharacterEggInfoRemover();
        CharacterEnemiesKilledRemover();
        CharacterStageDataRemover();
        CharacterSurvivedMinutesRemover();
        OpenedCoffinsRemover();
        SelectedSkinsRemover();
        SelectedSkinsV2Remover();
        StageCompletionLogRemover();
        UnlockedCharactersRemover();
        UnlockedSkinsRemover();
        UnlockedSkinsV2Remover();
    }

    private PlayerOptionsData DefaultPod(PlayerOptionsData pod)
    {
        PlayerOptionsData basePod = new();
        foreach (PropertyInfo propertyInfo in pod.GetType().GetProperties())
        { 
            if(_doNotCopy.Contains(propertyInfo.Name) || propertyInfo.Name.Contains("BackingField")) 
                continue;
            if (!propertyInfo.TryGetValue(pod, out var value)) continue;
            basePod.GetType().GetProperty(propertyInfo.Name)?.SetValue(basePod, value);
        }
        return basePod; 
    }

    void BoughtCharactersRemover()
    {
        ProcessIl2CppCharacterList(_staticPod.BoughtCharacters, _cleansedPod.BoughtCharacters, BoughtCharacters, 
            c => c, c => CustomCharacterNames[c]);
        ObjectToWrite["boughtCharacters"] = JArray.FromObject(BoughtCharacters);
    }

    void CharacterEggCountRemover()
    {
        ProcessIl2CppCharacterDictionary(_staticPod.CharacterEggCount, _cleansedPod.CharacterEggCount, 
            CharacterEggCount, "characterEggCount");
    }

    void CharacterEggInfoRemover()
    {
        ProcessCharacterDictionaryWithConversion(
            _staticPod.CharacterEggInfo, _cleansedPod.CharacterEggInfo, 
            CharacterEggInfo, "characterEggInfo", ConvertEggInfoToJObject);
    }

    private JObject ConvertEggInfoToJObject(Il2CppSystem.Collections.Generic.Dictionary<string, float> eggInfo)
    {
        JObject jObject = new();
        foreach (Il2CppSystem.Collections.Generic.KeyValuePair<string, float> kvp in eggInfo)
        {
            jObject.Add(kvp.Key, kvp.Value);
        }
        return jObject;
    }

    void CharacterEnemiesKilledRemover()
    {
        ProcessIl2CppCharacterDictionary(_staticPod.CharacterEnemiesKilled, _cleansedPod.CharacterEnemiesKilled, 
            CharacterEnemiesKilled, "characterEnemiesKilled");
    }

    void CharacterStageDataRemover()
    {
        ProcessCharacterDictionaryWithConversion(
            _staticPod.CharacterStageData, _cleansedPod.CharacterStageData, 
            CharacterStageData, "characterStageData", ConvertStageDataListToJArray);
    }

    private JArray ConvertStageDataListToJArray(Il2CppSystem.Collections.Generic.List<CharacterStageData> stageDataList)
    {
        JArray jArray = new();
        foreach (CharacterStageData characterStageData in stageDataList)
        {
            JObject jObject = new JObject
            {
                ["complete"] = characterStageData.complete,
                ["hurry"] = characterStageData.hurry,
                ["hyper"] = characterStageData.hyper,
                ["inverse"] = characterStageData.inverse,
                ["startedRun"] = characterStageData.startedRun,
                ["survivedMinutes"] = characterStageData.survivedMinutes,
                ["type"] = characterStageData.type.ToString()
            };
            jArray.Add(jObject);
        }
        return jArray;
    }

    void CharacterSurvivedMinutesRemover()
    {
        ProcessIl2CppCharacterDictionary(_staticPod.CharacterSurvivedMinutes, _cleansedPod.CharacterSurvivedMinutes, 
            CharacterSurvivedMinutes, "characterSurvivedMinutes");
    }

    void OpenedCoffinsRemover()
    {
        ProcessIl2CppCharacterList(_staticPod.OpenedCoffins, _cleansedPod.OpenedCoffins, OpenedCoffins, 
            c => c, c => CustomCharacterNames[c]);
        ObjectToWrite["openedCoffins"] = JArray.FromObject(OpenedCoffins);
    }

    void SelectedSkinsRemover()
    {
        ProcessIl2CppCharacterDictionary(_staticPod.SelectedSkins, _cleansedPod.SelectedSkins, 
            SelectedSkins, "selectedSkins");
    }

    void SelectedSkinsV2Remover()
    {
        ProcessCharacterDictionaryWithConversion(_staticPod.SelectedSkinsV2, _cleansedPod.SelectedSkinsV2, 
            SelectedSkinsV2, "selectedSkinsV2", value => value.ToString());
    }

    void StageCompletionLogRemover()
    {
        ProcessCharacterDictionaryWithConversion(
            _staticPod.StageCompletionLog, _cleansedPod.StageCompletionLog, 
            StageCompletionLog, "stageCompletionLog", ConvertStageListToJArray);
    }

    private JArray ConvertStageListToJArray(Il2CppSystem.Collections.Generic.List<StageType> stageList)
    {
        JArray jArray = new();
        foreach (StageType stage in stageList)
        {
            jArray.Add(stage.ToString());
        }
        return jArray;
    }

    void UnlockedCharactersRemover()
    {
        ProcessIl2CppCharacterList(_staticPod.UnlockedCharacters, _cleansedPod.UnlockedCharacters, UnlockedCharacters, 
            c => c, c => CustomCharacterNames[c]);
        ObjectToWrite["unlockedCharacters"] = JArray.FromObject(UnlockedCharacters);
    }

    void UnlockedSkinsRemover()
    {
        ProcessCharacterDictionaryWithConversion(
            _staticPod.UnlockedSkins, _cleansedPod.UnlockedSkins, 
            UnlockedSkins, "unlockedSkins", ConvertSkinListToJArray);
    }

    void UnlockedSkinsV2Remover()
    {
        ProcessCharacterDictionaryWithConversion(
            _staticPod.UnlockedSkinsV2, _cleansedPod.UnlockedSkinsV2, 
            UnlockedSkinsV2, "unlockedSkinsV2", ConvertSkinListToJArray);
    }

    private JArray ConvertSkinListToJArray(Il2CppSystem.Collections.Generic.List<SkinType> skinList)
    {
        JArray jArray = new();
        foreach (SkinType skin in skinList)
        {
            jArray.Add(skin);
        }
        return jArray;
    }

    // Setter Methods
    private void ProcessAllSetterMethods(JObject jObject)
    {
        BoughtCharactersSetter(jObject["boughtCharacters"] as JArray ?? new JArray());
        CharacterEggCountSetter(jObject["characterEggCount"] as JObject ?? new JObject());
        CharacterEggInfoSetter(jObject["characterEggInfo"] as JObject ?? new JObject());
        CharacterEnemiesKilledSetter(jObject["characterEnemiesKilled"] as JObject ?? new JObject());
        CharacterStageDataSetter(jObject["characterStageData"] as JObject ?? new JObject());
        CharacterSurvivedMinutesSetter(jObject["characterSurvivedMinutes"] as JObject ?? new JObject());
        OpenedCoffinsSetter(jObject["openedCoffins"] as JArray ?? new JArray());
        SelectedSkinsSetter(jObject["selectedSkins"] as JObject ?? new JObject());
        SelectedSkinsV2Setter(jObject["selectedSkinsV2"] as JObject ?? new JObject());
        StageCompletionLogSetter(jObject["stageCompletionLog"] as JObject ?? new JObject());
        UnlockedCharactersSetter(jObject["unlockedCharacters"] as JArray ?? new JArray());
        UnlockedSkinsSetter(jObject["unlockedSkins"] as JObject ?? new JObject());
        UnlockedSkinsV2Setter(jObject["unlockedSkinsV2"] as JObject ?? new JObject());
    }

    void BoughtCharactersSetter(JArray jArray)
    {
        ProcessCharacterListSetter(jArray, _writtenPod.BoughtCharacters, "boughtCharacters");
    }

    void CharacterEggCountSetter(JObject jObject)
    {
        ProcessCharacterDictionarySetter(jObject, _writtenPod.CharacterEggCount, 
            token => token?.Value<float?>() ?? 0f, 0f, "characterEggCount");
    }

    void CharacterEggInfoSetter(JObject jObject)
    {
        foreach (KeyValuePair<string, JToken> kvp in jObject)
        {
            if (CustomCharacterIDs.TryGetValue(kvp.Key, out var key))
            {
                var eggInfo = new Il2CppSystem.Collections.Generic.Dictionary<string, float>();
                if (kvp.Value is JObject nestedObject)
                {
                    foreach (KeyValuePair<string, JToken> nestedKvp in nestedObject)
                    {
                        var value = nestedKvp.Value?.Value<float>() ?? 0f;
                        eggInfo.Add(nestedKvp.Key, value);
                    }
                }
                _writtenPod.CharacterEggInfo[key] = eggInfo;
            }
            else if (IsUnclaimedCharacter(kvp.Key))
            {
                AddToUnclaimedData(kvp.Key, "characterEggInfo", kvp.Value ?? new JObject());
            }
        }
    }

    void CharacterEnemiesKilledSetter(JObject jObject)
    {
        ProcessCharacterDictionarySetter(jObject, _writtenPod.CharacterEnemiesKilled, 
            token => token?.Value<int?>() ?? 0, 0, "characterEnemiesKilled");
    }

    void CharacterStageDataSetter(JObject jObject)
    {
        foreach (KeyValuePair<string, JToken> kvp in jObject)
        {
            if (CustomCharacterIDs.TryGetValue(kvp.Key, out var key))
            {
                var data = new Il2CppSystem.Collections.Generic.List<CharacterStageData>();
                if (kvp.Value is JArray jsonArray)
                {
                    foreach (JToken token in jsonArray)
                    {
                        if (token is not JObject jobject) continue;
                        
                        var stageData = new CharacterStageData();
                        if (!Enum.TryParse(jobject["type"]?.Value<string>(), out StageType type)) continue;
                        
                        stageData.type = type;
                        stageData.complete = jobject["complete"]?.Value<int?>() ?? 0;
                        stageData.hurry = jobject["hurry"]?.Value<bool?>() ?? false;
                        stageData.hyper = jobject["hyper"]?.Value<bool?>() ?? false;
                        stageData.inverse = jobject["inverse"]?.Value<bool?>() ?? false;
                        stageData.startedRun = jobject["startedRun"]?.Value<int?>() ?? 0;
                        stageData.survivedMinutes = jobject["survivedMinutes"]?.Value<int?>() ?? 0;
                        
                        data.Add(stageData);
                    }
                }
                _writtenPod.CharacterStageData[key] = data;
            }
            else if (IsUnclaimedCharacter(kvp.Key))
            {
                AddToUnclaimedData(kvp.Key, "characterStageData", kvp.Value ?? new JArray());
            }
        }
    }

    void CharacterSurvivedMinutesSetter(JObject jObject)
    {
        ProcessCharacterDictionarySetter(jObject, _writtenPod.CharacterSurvivedMinutes, 
            token => token?.Value<int?>() ?? 0, 0, "characterSurvivedMinutes");
    }

    void OpenedCoffinsSetter(JArray jArray)
    {
        ProcessCharacterListSetter(jArray, _writtenPod.OpenedCoffins, "openedCoffins");
    }

    void SelectedSkinsSetter(JObject jObject)
    {
        ProcessCharacterDictionarySetter(jObject, _writtenPod.SelectedSkins, 
            token => token?.Value<int?>() ?? 0, 0, "selectedSkins");
    }

    void SelectedSkinsV2Setter(JObject jObject)
    {
        ProcessCharacterDictionarySetter(jObject, _writtenPod.SelectedSkinsV2, 
            token => ParseEnumWithFallback<SkinType>(token?.Value<string>(), default), default, "selectedSkinsV2");
    }

    void StageCompletionLogSetter(JObject jObject)
    {
        ProcessCharacterListWithEnumSetter(jObject, _writtenPod.StageCompletionLog, 
            token => ParseEnumWithFallback<StageType>(token?.Value<string>(), default), "stageCompletionLog");
    }

    void UnlockedCharactersSetter(JArray jArray)
    {
        ProcessCharacterListSetter(jArray, _writtenPod.UnlockedCharacters, "unlockedCharacters");
    }

    void UnlockedSkinsSetter(JObject jObject)
    {
        ProcessCharacterListWithEnumSetter(jObject, _writtenPod.UnlockedSkins, 
            token => ParseEnumWithFallback<SkinType>(token?.Value<string>(), default), "unlockedSkins");
    }

    void UnlockedSkinsV2Setter(JObject jObject)
    {
        ProcessCharacterListWithEnumSetter(jObject, _writtenPod.UnlockedSkinsV2, 
            token => ParseEnumWithFallback<SkinType>(token?.Value<string>(), default), "unlockedSkinsV2");
    }
}