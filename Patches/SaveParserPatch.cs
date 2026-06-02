using CoffinTech.SaveData;
using HarmonyLib;
using Il2CppVampireSurvivors.Framework.Saves;

namespace CoffinTech.Patches;

[HarmonyPatch(typeof(SaveParser))]
static class SaveParserPatch
{
    /// <summary>
    /// Prefix patch that restores mod data after vanilla save parsing.
    /// </summary>
    [HarmonyPatch(nameof(SaveParser.PostParseFixes))]
    [HarmonyPrefix]
    public static void PostParseFixes(SaveParser __instance)
    {
        if (__instance == null) return;
        ModOptionsData modOptionsData = new();
        __instance._pod = modOptionsData.ModDataSetter(__instance._pod);
    }
}