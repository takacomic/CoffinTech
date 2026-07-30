using HarmonyLib;
using Il2CppVampireSurvivors;
using MelonLoader;

namespace CoffinTech.Patches;

[HarmonyPatch(typeof(EnumCache))]
public class SerializationUtilsPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(EnumCache.GetSerializationTypeForEnum))]
    public static void Postfix(Type enumType, ref SerializationType __result)
    {
        //MelonLogger.Msg($"SerialType: {__result.ToString()}, EnumType: {nameof(enumType)}");
    }
}