using CoffinTech.Logger;
using HarmonyLib;
using Il2CppVampireSurvivors.Data;

namespace CoffinTech.Patches;

[HarmonyPatch(typeof(PlayerOptionsData))]
public static class PlayerOptionsDataPatch
{
    [HarmonyPatch(nameof(PlayerOptionsData.Equals))]
    [HarmonyPrefix]
    public static bool Prefix(PlayerOptionsData __instance, ref bool __result)
    {
        DebugLogger.Msg($"Checksum is valid{__result}");
        if (!CoffinTechMod.BypassChecksum) return true;
        
        __result = true;
        return false;
    }
}
