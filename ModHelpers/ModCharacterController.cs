using CoffinTech.SaveData;
using HarmonyLib;
using Il2CppVampireSurvivors;
using Il2CppVampireSurvivors.Data;
using Il2CppVampireSurvivors.Objects.Characters;
using MelonLoader;

namespace CoffinTech.Utils;

[HarmonyPatch(typeof(CharacterController))]
static class HarmonyCharacterController
{
    internal static CharacterController? _characterController;
    
    [HarmonyPatch(nameof(CharacterController.AfterFullInitialization))]
    [HarmonyPostfix]
    // ReSharper disable InconsistentNaming
    public static void AfterFullInitialization(CharacterController __instance)
    {
        _characterController = __instance;
        ModCharacterControllerRegistry.InvokeAfterFullInit(__instance);
    }
    
    [HarmonyPatch(nameof(CharacterController.HandleLateUpdate))]
    [HarmonyPostfix]
    public static void HandleLateUpdate(CharacterController __instance)
    {
        ModCharacterControllerRegistry.InvokeHandleLateUpdate(__instance);
    }
    
    [HarmonyPatch(nameof(CharacterController.LevelUp))]
    [HarmonyPostfix]
    public static void LevelUp(CharacterController __instance)
    {
        ModCharacterControllerRegistry.InvokeLevelUp(__instance);
    }
    
    [HarmonyPatch(nameof(CharacterController.OnStop))]
    [HarmonyPostfix]
    public static void OnStop(CharacterController __instance)
    {
        ModCharacterControllerRegistry.InvokeOnStop(__instance);
    }
    [HarmonyPatch(nameof(CharacterController.OnUpdate))]
    [HarmonyPostfix]
    public static void OnUpdate(CharacterController __instance)
    {
        ModCharacterControllerRegistry.InvokeOnUpdate(__instance);
    }
}

[HarmonyPatch(typeof(EnemyController))]
static class HarmonyEnemyController
{
    [HarmonyPatch(nameof(EnemyController.OnPlayerOverlap))]
    [HarmonyPostfix]
    public static void OnPlayerOverlap(EnemyController __instance, CharacterController player)
    {
        ModCharacterControllerRegistry.InvokeEnemyOnPlayerOverlap(__instance, player);
    }
}

public static class ModCharacterControllerRegistry
{
    private static readonly Dictionary<string, ModCharacterController> ModCharacterControllers = new ();
    
    public static void Register(ModCharacterController modCharacterController, string internalName)
    {
        ModCharacterControllers.Add(internalName, modCharacterController);
    }

    public static CharacterType RegisterForType(ModCharacterController modCharacterController, string internalName)
    {
        var characterType = ModOptionsData.CustomCharacter(internalName).Value;
        ModCharacterControllers.Add(internalName, modCharacterController);
        return characterType;
    }
    
    public static void Unregister(string characterId)
    {
        ModCharacterControllers.Remove(characterId);
    }

    public static void Unregister(CharacterType characterType)
    {
        ModOptionsData.TryGetCustomCharacter(null, characterType, out var character);
        ModCharacterControllers.Remove(character.Key);
    }

    internal static void InvokeOnStop(CharacterController instance)
    {
        if (TryGetController(instance, out var modCharacterController))
            modCharacterController?.OnStop(instance);
    }
    
    internal static void InvokeAfterFullInit(CharacterController instance)
    {
        if (TryGetController(instance, out var modCharacterController))
            modCharacterController?.AfterFullInit(instance);
    }
    
    internal static void InvokeOnUpdate(CharacterController instance)
    {
        if (TryGetController(instance, out var modCharacterController))
            modCharacterController?.OnUpdate(instance);
    }
    
    internal static void InvokeHandleLateUpdate(CharacterController instance)
    {
        if (TryGetController(instance, out var modCharacterController))
            modCharacterController?.HandleLateUpdate(instance);
    }
    
    internal static void InvokeLevelUp(CharacterController instance)
    {
        if (TryGetController(instance, out var modCharacterController))
            modCharacterController?.LevelUp(instance);
    }
    
    internal static void InvokeEnemyOnPlayerOverlap(EnemyController instance, CharacterController player)
    {
        if (TryGetController(player, out var modCharacterController))
            modCharacterController?.EnemyOnPlayerOverlap(instance, player);
    }
    
    

    private static bool TryGetController(CharacterController instance, out ModCharacterController? modCharacterController)
    {
        modCharacterController = null;
        if (!ModOptionsData.TryGetCustomCharacter(null, instance._characterType, out var character)) return false;
        return instance != null && ModCharacterControllers.TryGetValue(character.Key, out modCharacterController);
    }
}

public abstract class ModCharacterController
{
    private static readonly Dictionary<Type, ModCharacterController> Instances = new();

    public static T GetInstance<T>() where T : ModCharacterController, new()
    {
        var type = typeof(T);
        if (Instances.TryGetValue(type, out var instance))
            return (T)instance;

        var newInstance = new T();
        Instances[type] = newInstance;
        return newInstance;
    }
    
    public virtual void AfterFullInit(CharacterController instance)
    {
        
    }
    
    public virtual void HandleLateUpdate(CharacterController instance)
    {
        
    }
    
    public virtual void LevelUp(CharacterController instance)
    {
        
    }
    
    public virtual void OnStop(CharacterController instance)
    {
        
    }
    
    public virtual void OnUpdate(CharacterController instance)
    {
        
    }
    
    public virtual void EnemyOnPlayerOverlap(EnemyController instance, CharacterController player)
    {
        
    }
}
