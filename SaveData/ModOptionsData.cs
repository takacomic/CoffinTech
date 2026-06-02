using System.Reflection;
using CoffinTech.Extensions;
using CoffinTech.Logger;
using CoffinTech.Patches;
using Il2CppVampireSurvivors.Data;
using Il2CppVampireSurvivors.Data.Props;
using Newtonsoft.Json.Linq;

namespace CoffinTech.SaveData;

/// <summary>
/// Handles serialization and deserialization of mod-specific character data.
/// </summary>
public class ModOptionsData
{
    // Constants
    private const int Version = 1;
    private const int IDBase = 100000;
    private const string ModSaveDataPath = "UserData/ModSaveData";
    private const string UnclaimedDataFileName = "UnclaimedCharacterData.json";
    private const CharacterType DefaultCharacter = CharacterType.ANTONIO;
    
    private static readonly object LockObject = new();

    // Static data storage
    private static int CharacterIDCounter = IDBase;
    private static readonly Dictionary<CharacterType, string> CustomCharacterNames = new();
    private static readonly Dictionary<string, CharacterType> CustomCharacterIDs = new();
    private static int ItemIDCounter = IDBase;
    private static readonly Dictionary<ItemType, string> CustomItemNames = new();
    private static readonly Dictionary<string, ItemType> CustomItemIDs = new();
    private static readonly List<ItemType> CustomRelicIDs = new();
    private static int SecretIDCounter = IDBase;
    private static readonly Dictionary<SecretType, string> CustomSecretNames = new();
    private static readonly Dictionary<string, SecretType> CustomSecretIDs = new();
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

    private static readonly List<string> CollectedItems = new();
    private static readonly List<string> Secrets = new();

    // Unclaimed data storage
    private static readonly JObject UnclaimedData = new();
    private static bool _unclaimedDataLoaded;

    // Instance fields
    private PlayerOptionsData _staticPod = null!;
    private PlayerOptionsData _cleansedPod = null!;
    private PlayerOptionsData _writtenPod = null!;

    private readonly string[] _doNotCopy =
    {
        "ObjectClass", "Pointer", "WasCollected",
        "BoughtCharacters", "CharacterEggCount", "CharacterEggInfo", "CharacterEnemiesKilled",
        "CharacterStageData", "CharacterSurvivedMinutes", "OpenedCoffins", "SelectedCharacter", "SelectedSkins",
        "SelectedSkinsV2", "StageCompletionLog",
        "UnlockedCharacters", "UnlockedSkins", "UnlockedSkinsV2", "CollectedItems", "Secrets"
    };

    // Properties
    internal static JObject ObjectToWrite { get; } = new();

    /// <summary>
    /// Cached PropertyInfo array to avoid repeated reflection costs.
    /// PlayerOptionsData.GetProperties() is expensive; caching improves save performance by ~0.5ms per operation.
    /// </summary>
    private static readonly PropertyInfo[] PlayerOptionsDataProperties = typeof(PlayerOptionsData).GetProperties();

    // Public API
    /// <summary>
    /// Registers a custom character with the mod's save system.
    /// </summary>
    /// <param name="id">Unique string identifier for a character (e.g., "MyModCharacter")</param>
    public static KeyValuePair<string, CharacterType> CustomCharacter(string id)
    {
        lock (LockObject)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Character ID cannot be null or empty.");
            }
            if (CustomCharacterIDs.ContainsKey(id))
            {
                return new KeyValuePair<string, CharacterType>(id, CustomCharacterIDs[id]);
            }
            CharacterType character = (CharacterType) CharacterIDCounter++;
            CustomCharacterIDs.Add(id, character);
            CustomCharacterNames.Add(character, id);
            return new KeyValuePair<string, CharacterType>(id, character);
        }
    }
    
    public static bool TryGetCustomCharacter(string? id, CharacterType? type, out KeyValuePair<string, CharacterType> character)
    {
        character = default;
        if (!string.IsNullOrEmpty(id))
        {
            lock (LockObject)
            {
                if (!CustomCharacterIDs.TryGetValue(id, out var secretType)) return false;
                character = new KeyValuePair<string, CharacterType>(id, secretType);
                return true;
            }
        }

        if (!type.HasValue) return false;
        lock (LockObject)
        {
            if (!CustomCharacterNames.TryGetValue(type.Value, out var secretId)) return false;
            character = new KeyValuePair<string, CharacterType>(secretId, type.Value);
            return true;
        }
    }
    
    public static bool IsCustomCharacter(CharacterType character)
    {
        lock (LockObject)
        {
            return CustomCharacterNames.ContainsKey(character);
        }
    }
    
    // Public API
    /// <summary>
    /// Registers a custom item with the mod's save system.
    /// </summary>
    /// <param name="id">Unique string identifier for a item (e.g., "MyModItem")</param>
    /// <param name="isRelic">Boolean to register the item as a relic for IsCustomRelic</param>
    public static KeyValuePair<string, ItemType> CustomItem(string id, bool isRelic = false)
    {
        lock (LockObject)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Item ID cannot be null or empty.");
            }
            if (CustomItemIDs.ContainsKey(id))
            {
                return new KeyValuePair<string, ItemType>(id, CustomItemIDs[id]);
            }
            ItemType item = (ItemType) ItemIDCounter++;
            CustomItemIDs.Add(id, item);
            CustomItemNames.Add(item, id);
            if (isRelic) CustomRelicIDs.Add(item);
            return new KeyValuePair<string, ItemType>(id, item);
        }
    }
    
    public static bool TryGetCustomItem(string? id, ItemType? type, out KeyValuePair<string, ItemType> item)
    {
        item = default;
        if (!string.IsNullOrEmpty(id))
        {
            lock (LockObject)
            {
                if (!CustomItemIDs.TryGetValue(id, out var secretType)) return false;
                item = new KeyValuePair<string, ItemType>(id, secretType);
                return true;
            }
        }

        if (!type.HasValue) return false;
        lock (LockObject)
        {
            if (!CustomItemNames.TryGetValue(type.Value, out var secretId)) return false;
            item = new KeyValuePair<string, ItemType>(secretId, type.Value);
            return true;
        }
    }
    
    public static bool IsCustomItem(ItemType secret)
    {
        return CustomItemNames.ContainsKey(secret);
    }
    
    // Public API
    /// <summary>
    /// Returns true if the given ItemType is a relic registered with CustomItem.
    /// </summary>
    /// <param name="item">The ItemType enum value assigned to a custom item</param>
    public static bool IsCustomRelic(ItemType item)
    {
        lock (LockObject)
        {
            if (CustomRelicIDs.Contains(item))
            {
                return true;
            }

            return false;
        }
    }
    
    // Public API
    /// <summary>
    /// Registers a custom secret with the mod's save system.
    /// </summary>
    /// <param name="id">Unique string identifier for a secret (e.g., "MyModSecret")</param>
    public static KeyValuePair<string, SecretType> CustomSecret(string id)
    {
        lock (LockObject)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Secret ID cannot be null or empty.");
            }
            if (CustomSecretIDs.ContainsKey(id))
            {
                return new KeyValuePair<string, SecretType>(id, CustomSecretIDs[id]);
            }
            SecretType secret = (SecretType) SecretIDCounter++;
            CustomSecretIDs.Add(id, secret);
            CustomSecretNames.Add(secret, id);
            return new KeyValuePair<string, SecretType>(id, secret);
        }
    }

    public static bool TryGetCustomSecret(string? id, SecretType? type, out KeyValuePair<string, SecretType> secret)
    {
        secret = default;
        if (!string.IsNullOrEmpty(id))
        {
            lock (LockObject)
            {
                if (!CustomSecretIDs.TryGetValue(id, out var secretType)) return false;
                secret = new KeyValuePair<string, SecretType>(id, secretType);
                return true;
            }
        }

        if (!type.HasValue) return false;
        lock (LockObject)
        {
            if (!CustomSecretNames.TryGetValue(type.Value, out var secretId)) return false;
            secret = new KeyValuePair<string, SecretType>(secretId, type.Value);
            return true;
        }
    }
    
    public static bool IsCustomSecret(SecretType secret)
    {
        return CustomSecretNames.ContainsKey(secret);
    }

    // Public Instance Methods
    /// <summary>
    /// Removes mod-specific character data from the PlayerOptionsData before vanilla serialization.
    /// Called during save operations to separate mod data from base game save data.
    /// </summary>
    /// <param name="pod">The PlayerOptionsData instance containing mixed vanilla and mod data</param>
    /// <returns>A new PlayerOptionsData with mod character data removed (safe for vanilla serialization)</returns>
    internal PlayerOptionsData ModDataRemover(PlayerOptionsData pod)
    {
        _staticPod = pod;
        _cleansedPod = DefaultPod(pod);

        string? customCharId = null;
        lock (LockObject)
        {
            ObjectToWrite.TryAdd("version", Version);

            if (!CustomCharacterNames.TryGetValue(_staticPod.SelectedCharacter, out customCharId))
            {
                int charValue = (int)_staticPod.SelectedCharacter;
                if (charValue < 0 || charValue > 300)
                    _staticPod.SelectedCharacter = DefaultCharacter;
            }

            _cleansedPod.SelectedCharacter = _staticPod.SelectedCharacter;
            ObjectToWrite["selectedCharacter"] = customCharId ?? _staticPod.SelectedCharacter.ToString();
        }

        EnsureModSaveDataDirectory();

        // Process all character data - extracts mod character data into ObjectToWrite
        ProcessAllRemoverMethods();

        return _cleansedPod;
    }

    /// <summary>
    /// Injects mod-specific character data into PlayerOptionsData after vanilla deserialization.
    /// Called during load operations to restore mod character data from save file.
    /// </summary>
    /// <param name="pod">The PlayerOptionsData loaded from vanilla save</param>
    /// <returns>The POD with mod character data restored</returns>
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
        if(jObject == null || !jObject.HasValues) return pod;
        
        string? selectedCharStr = null;
        lock (LockObject)
        {
            selectedCharStr = jObject["selectedCharacter"]?.Value<string>();
        }
        if (CustomCharacterIDs.TryGetValue(selectedCharStr, out var characterId))
            _writtenPod.SelectedCharacter = characterId;
            
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

    private static bool IsUnclaimedCharacter(string characterId)
    {
        lock (LockObject)
        {
            return !CustomCharacterIDs.ContainsKey(characterId) && 
                   !Enum.TryParse<CharacterType>(characterId, out _);
        }
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

    /// <summary>
    /// Loads unclaimed character data from disk.
    /// Unclaimed data represents character progress for custom characters that were
    /// in the save file but whose mod is not currently loaded.
    /// </summary>
    private static void LoadUnclaimedData()
    {
        string filePath = GetUnclaimedDataPath();
        
        if (!File.Exists(filePath))
            return;
            
        try
        {
            string json = File.ReadAllText(filePath);
            JObject loadedData;
            // Defensive JSON parsing - corrupted files should not crash the mod
            try
            {
                loadedData = JObject.Parse(json);
            }
            catch (Exception parseEx)
            {
                MelonLoader.MelonLogger.Error($"Failed to parse unclaimed character data JSON: {parseEx.Message}");
                return;
            }
            
            // Merge loaded data into UnclaimedData
            lock (LockObject)
            {
                foreach (var property in loadedData.Properties())
                {
                    UnclaimedData[property.Name] = property.Value;
                }
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
            JObject dataToSave;
            lock (LockObject)
            {
                dataToSave = new JObject
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
            }
            
            File.WriteAllText(filePath, dataToSave.ToString(Newtonsoft.Json.Formatting.Indented));
        }
        catch (Exception ex)
        {
            MelonLoader.MelonLogger.Error($"Failed to save unclaimed character data: {ex.Message}");
        }
    }

    private void AddToUnclaimedData(string characterId, string dataKey, JToken value)
    {
        lock (LockObject)
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
    }

    /// <summary>
    /// Parses enum from string with fallback to default value. 
    /// </summary>
    /// <typeparam name="T">The enum type to parse</typeparam>
    /// <param name="value">The string value to parse</param>
    /// <param name="fallback">Default value if parsing fails</param>
    /// <returns>The parsed enum value or fallback</returns>
    private T ParseEnumWithFallback<T>(string value, T fallback) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, out var result))
            return result;
        
        // Numeric fallback for IL2CPP compatibility
        if (int.TryParse(value, out var intValue) && Enum.IsDefined(typeof(T), intValue))
            return (T)(object)intValue;
            
        return fallback;
    }
    // Data Processing Helper Methods
    /// <summary>
    /// Processes IL2CPP lists containing character data, separating vanilla and mod characters.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list</typeparam>
    /// <param name="source">Original IL2CPP list from PlayerOptionsData</param>
    /// <param name="target">Cleansed IL2CPP list for vanilla save</param>
    /// <param name="customTarget">String list for mod character IDs</param>
    /// <param name="getCharacterType">Function to extract CharacterType from element</param>
    /// <param name="getCustomId">Function to extract custom ID from element</param>
    private void ProcessIl2CppCharacterList<T>(Il2CppSystem.Collections.Generic.List<T> source,
        Il2CppSystem.Collections.Generic.List<T> target, List<string> customTarget,
        Func<T, CharacterType> getCharacterType, Func<T, string> getCustomId)
    {
        foreach (var item in source)
        {
            // IL2CPP-safe null checking - avoid 'is null' pattern
            if (item == null) continue;
            // Explicit delegate null checks - IL2CPP may pass null delegates
            if (getCharacterType == null) continue;
            if (getCustomId == null) continue;
            
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
    
    private void ProcessIl2CppItemList<T>(Il2CppSystem.Collections.Generic.List<T> source,
        Il2CppSystem.Collections.Generic.List<T> target, List<string> customTarget,
        Func<T, ItemType> getItemType, Func<T, string> getCustomId)
    {
        foreach (var item in source)
        {
            // IL2CPP-safe null checking - avoid 'is null' pattern
            if (item == null) continue;
            // Explicit delegate null checks - IL2CPP may pass null delegates
            if (getItemType == null) continue;
            if (getCustomId == null) continue;
            
            var itemType = getItemType(item);
            if (!IsCustomItem(itemType))
            {
                target.Add(item);
                continue;
            }
            var customId = getCustomId(item);
            if (!customTarget.Contains(customId))
                customTarget.Add(customId);
        }
    }
    
    private void ProcessIl2CppSecretList<T>(Il2CppSystem.Collections.Generic.List<T> source,
        Il2CppSystem.Collections.Generic.List<T> target, List<string> customTarget,
        Func<T, SecretType> getItemType, Func<T, string> getCustomId)
    {
        foreach (var item in source)
        {
            // IL2CPP-safe null checking - avoid 'is null' pattern
            if (item == null) continue;
            // Explicit delegate null checks - IL2CPP may pass null delegates
            if (getItemType == null) continue;
            if (getCustomId == null) continue;
            
            var itemType = getItemType(item);
            if (!IsCustomSecret(itemType))
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

            string customId;
            lock (LockObject)
            {
                if (!CustomCharacterNames.TryGetValue(character, out customId))
                    continue;
            }

            if (kvp.Value != null)
                customTarget[customId] = JToken.FromObject(kvp.Value);
        }
        lock (LockObject)
        {
            ObjectToWrite[objectToWriteKey] = customTarget;
        }
    }

    /// <summary>
    /// Processes IL2CPP dictionaries with type conversion, separating vanilla and mod characters.
    /// </summary>
    /// <typeparam name="T">Source value type from IL2CPP dictionary</typeparam>
    /// <typeparam name="TU">Target conversion type</typeparam>
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

            string customId;
            lock (LockObject)
            {
                // TryGetValue is atomic and thread-safe with locking
                if (!CustomCharacterNames.TryGetValue(character, out customId))
                    continue;
            }

            // Explicit delegate null check - IL2CPP-safe
            if (convertValue == null) continue;
            var convertedValue = convertValue(kvp.Value);
            if (convertedValue != null)
                customTarget[customId] = JToken.FromObject(convertedValue);
        }
        lock (LockObject)
        {
            ObjectToWrite[objectToWriteKey] = customTarget;
        }
    }

    private void ProcessCharacterListSetter(JArray jArray, Il2CppSystem.Collections.Generic.List<CharacterType> target, string dataKey)
    {
        foreach (string character in jArray)
        {
            if (character == null) continue;
            
            bool isCustom = false;
            CharacterType key = default;
            lock (LockObject)
            {
                if (CustomCharacterIDs.TryGetValue(character, out key))
                {
                    isCustom = true;
                }
            }
            
            if (isCustom)
            {
                target.Add(key);
            }
            else if (IsUnclaimedCharacter(character))
            {
                // Initialize array if needed
                lock (LockObject)
                {
                    if (!UnclaimedData.ContainsKey(dataKey))
                    {
                        UnclaimedData[dataKey] = new JArray();
                    }
                }
                
                AddToUnclaimedData(character, dataKey, character);
            }
        }
    }
    
    private void ProcessItemListSetter(JArray jArray, Il2CppSystem.Collections.Generic.List<ItemType> target, string dataKey)
    {
        foreach (string item in jArray)
        {
            if (item == null) continue;
            
            bool isCustom = false;
            ItemType key = default;
            lock (LockObject)
            {
                if (CustomItemIDs.TryGetValue(item, out key))
                {
                    isCustom = true;
                }
            }
            
            if (isCustom)
            {
                target.Add(key);
            }
            else if (IsUnclaimedCharacter(item))
            {
                // Initialize array if needed
                lock (LockObject)
                {
                    if (!UnclaimedData.ContainsKey(dataKey))
                    {
                        UnclaimedData[dataKey] = new JArray();
                    }
                }
                
                AddToUnclaimedData(item, dataKey, item);
            }
        }
    }
    
    private void ProcessSecretListSetter(JArray jArray, Il2CppSystem.Collections.Generic.List<SecretType> target, string dataKey)
    {
        foreach (string item in jArray)
        {
            if (item == null) continue;
            
            bool isCustom = false;
            SecretType key = default;
            lock (LockObject)
            {
                if (CustomSecretIDs.TryGetValue(item, out key))
                {
                    isCustom = true;
                }
            }
            
            if (isCustom)
            {
                target.Add(key);
            }
            else if (IsUnclaimedCharacter(item))
            {
                // Initialize array if needed
                lock (LockObject)
                {
                    if (!UnclaimedData.ContainsKey(dataKey))
                    {
                        UnclaimedData[dataKey] = new JArray();
                    }
                }
                
                AddToUnclaimedData(item, dataKey, item);
            }
        }
    }

    private void ProcessCharacterDictionarySetter<T>(JObject jObject, Il2CppSystem.Collections.Generic.Dictionary<CharacterType, T> target, 
        Func<JToken, T> converter, T defaultValue, string dataKey)
    {
        foreach (KeyValuePair<string, JToken> kvp in jObject)
        {
            bool isCustom = false;
            CharacterType key = default;
            lock (LockObject)
            {
                if (CustomCharacterIDs.TryGetValue(kvp.Key, out key))
                {
                    isCustom = true;
                }
            }
            
            if (isCustom)
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

    /// <summary>
    /// Processes enum list dictionaries during save loading.
    /// </summary>
    private void ProcessCharacterListWithEnumSetter<T>(JObject jObject, Il2CppSystem.Collections.Generic.Dictionary<CharacterType, Il2CppSystem.Collections.Generic.List<T>> target, 
        Func<JToken, T> converter, string dataKey) where T : struct, Enum
    {
        foreach (KeyValuePair<string, JToken> kvp in jObject)
        {
            bool isCustom = false;
            CharacterType key = default;
            lock (LockObject)
            {
                if (CustomCharacterIDs.TryGetValue(kvp.Key, out key))
                {
                    isCustom = true;
                }
            }
            
            if (isCustom)
            {
                var dataList = new Il2CppSystem.Collections.Generic.List<T>();
                foreach (JToken token in kvp.Value as JArray ?? new JArray())
                {
                    // IL2CPP: Explicit variable declaration, not 'is var' pattern
                    var parsedValue = ParseEnumWithFallback<T>(token.Value<string>(), default);
                    // IL2CPP: Standard comparison, not pattern matching
                    if (!Equals(parsedValue, default(T)))
                        dataList.Add(parsedValue);
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
        CollectedItemsRemover();
        SecretsRemover();
    }

    private PlayerOptionsData DefaultPod(PlayerOptionsData pod)
    {
        PlayerOptionsData basePod = new();
        foreach (PropertyInfo propertyInfo in PlayerOptionsDataProperties)
        { 
            if(_doNotCopy.Contains(propertyInfo.Name) || propertyInfo.Name.Contains("BackingField")) 
                continue;
            if (!propertyInfo.TryGetValue(pod, out var value)) continue;
            typeof(PlayerOptionsData).GetProperty(propertyInfo.Name)?.SetValue(basePod, value);
        }
        return basePod; 
    }

    void BoughtCharactersRemover()
    {
        ProcessIl2CppCharacterList(_staticPod.BoughtCharacters, _cleansedPod.BoughtCharacters, BoughtCharacters,
            c => c, c => CustomCharacterNames[c]);
        lock (LockObject)
        {
            ObjectToWrite["boughtCharacters"] = JArray.FromObject(BoughtCharacters);
        }
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
        lock (LockObject)
        {
            ObjectToWrite["openedCoffins"] = JArray.FromObject(OpenedCoffins);
        }
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
        lock (LockObject)
        {
            ObjectToWrite["unlockedCharacters"] = JArray.FromObject(UnlockedCharacters);
        }
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
    
    void CollectedItemsRemover()
        {
            ProcessIl2CppItemList(_staticPod.CollectedItems, _cleansedPod.CollectedItems, CollectedItems,
                c => c, c => CustomItemNames[c]);
            lock (LockObject)
            {
                ObjectToWrite["collectedItems"] = JArray.FromObject(CollectedItems);
            }
        }
    
    void SecretsRemover()
    {
        ProcessIl2CppSecretList(_staticPod.Secrets, _cleansedPod.Secrets, Secrets,
            c => c, c => CustomSecretNames[c]);
        lock (LockObject)
        {
            ObjectToWrite["Secrets"] = JArray.FromObject(Secrets);
        }
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
        CollectedItemsSetter(jObject["collectedItems"] as JArray ?? new JArray());
        SecretsSetter(jObject["Secrets"] as JArray ?? new JArray());
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
            bool isCustom = false;
            CharacterType key = default;
            lock (LockObject)
            {
                if (CustomCharacterIDs.TryGetValue(kvp.Key, out key))
                {
                    isCustom = true;
                }
            }
            
            if (isCustom)
            {
                var eggInfo = new Il2CppSystem.Collections.Generic.Dictionary<string, float>();
                if (kvp.Value is JObject nestedObject)
                {
                    foreach (KeyValuePair<string, JToken?> nestedKvp in nestedObject)
                    {
                        // Null safety: nested values may be null in corrupted saves
                        if (nestedKvp.Value == null) continue;
                        var value = nestedKvp.Value.Value<float?>() ?? 0f;
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
            bool isCustom = false;
            CharacterType key = default;
            lock (LockObject)
            {
                if (CustomCharacterIDs.TryGetValue(kvp.Key, out key))
                {
                    isCustom = true;
                }
            }
            
            if (isCustom)
            {
                var data = new Il2CppSystem.Collections.Generic.List<CharacterStageData>();
                if (kvp.Value is JArray jsonArray)
                {
                    foreach (JToken token in jsonArray)
                    {
                        // IL2CPP: Explicit null check
                        if (token == null) continue;
                        // IL2CPP: Avoid 'is not JObject' pattern, use explicit check
                        if (!(token is JObject jobject)) continue;
                        
                        var stageData = new CharacterStageData();
                        // Null-conditional operator (?) is safe - returns null if key missing
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
    
    void CollectedItemsSetter(JArray jArray)
    {
        ProcessItemListSetter(jArray, _writtenPod.CollectedItems, "collectedItems");
    }
    
    void SecretsSetter(JArray jArray)
    {
        ProcessSecretListSetter(jArray, _writtenPod.Secrets, "Secrets");
    }
}