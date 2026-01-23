using HarmonyLib;
using Il2CppVampireSurvivors.Objects.Characters;

namespace CoffinTech.Tools;

[HarmonyPatch(typeof(CharacterController))]
static class HarmonyCharacterController
{
    [HarmonyPatch(nameof(CharacterController.AfterFullInitialization))]
    [HarmonyPostfix]
    // ReSharper disable InconsistentNaming
    public static void AfterFullInitialization(CharacterController __instance)
    {
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

public static class ModCharacterControllerRegistry
{
    private static readonly Dictionary<string, ModCharacterController> ModCharacterControllers = new ();

    public static void Register(ModCharacterController modCharacterController, string characterType)
    {
        ModCharacterControllers.TryAdd(characterType, modCharacterController);
    }

    public static void Unregister(string characterType)
    {
        ModCharacterControllers.Remove(characterType);
    }

    internal static void InvokeOnStop(CharacterController instance)
    {
        string characterType = instance._characterType.ToString();
        if (ModCharacterControllers.TryGetValue(characterType, out var modCharacterController))
            modCharacterController.OnStop(instance);
    }
    
    internal static void InvokeAfterFullInit(CharacterController instance)
    {
        string characterType = instance._characterType.ToString();
        if (ModCharacterControllers.TryGetValue(characterType, out var modCharacterController))
            modCharacterController.AfterFullInit(instance);
    }
    
    internal static void InvokeOnUpdate(CharacterController instance)
    {
        string characterType = instance._characterType.ToString();
        if (ModCharacterControllers.TryGetValue(characterType, out var modCharacterController))
            modCharacterController.OnUpdate(instance);
    }
    
    internal static void InvokeHandleLateUpdate(CharacterController instance)
    {
        string characterType = instance._characterType.ToString();
        if (ModCharacterControllers.TryGetValue(characterType, out var modCharacterController))
            modCharacterController.HandleLateUpdate(instance);
    }
    internal static void InvokeLevelUp(CharacterController instance)
    {
        string characterType = instance._characterType.ToString();
        if (ModCharacterControllers.TryGetValue(characterType, out var modCharacterController))
            modCharacterController.LevelUp(instance);
    }
}

public abstract class ModCharacterController
{
    private static readonly Dictionary<Type, ModCharacterController> Instances = new();

    public static T GetInstance<T>() where T : ModCharacterController, new()
    {
        Type type = typeof(T);
        if (!Instances.ContainsKey(type))
        {
            Instances[type] = new T();
        }
        return (T)Instances[type];
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
}