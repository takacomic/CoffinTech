using HarmonyLib;
using Il2CppVampireSurvivors.Data;
using Il2CppVampireSurvivors.Objects.Characters;

namespace CoffinTech.Utils;

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
    private static readonly Dictionary<CharacterType, ModCharacterController> ModCharacterControllers = new ();

    public static void Register(ModCharacterController modCharacterController, CharacterType characterType)
    {
        ModCharacterControllers[characterType] = modCharacterController;
    }

    public static void Unregister(CharacterType characterType)
    {
        ModCharacterControllers.Remove(characterType);
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

    private static bool TryGetController(CharacterController instance, out ModCharacterController? modCharacterController)
    {
        modCharacterController = null;
        return instance != null && ModCharacterControllers.TryGetValue(instance._characterType, out modCharacterController);
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
}
