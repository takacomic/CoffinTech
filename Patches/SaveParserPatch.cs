using CoffinTech.SaveData;
using HarmonyLib;
using Il2CppVampireSurvivors.Data;
using Il2CppVampireSurvivors.Framework.Saves;
using MelonLoader;

namespace CoffinTech.Patches;

[HarmonyPatch(typeof(SaveParser))]
static class SaveParserPatch
{
    private static int _parsed = 0;
    /// <summary>
    /// Prefix patch that restores mod data after vanilla save parsing.
    /// </summary>
    [HarmonyPatch(nameof(SaveParser.PostParseFixes))]
    [HarmonyPrefix]
    public static void PostParseFixes(SaveParser __instance)
    {
        if (__instance == null) return;
        _parsed++;
        if (_parsed != Enum.GetValues<AdventureType>().Length) return;
        ModOptionsData modOptionsData = new();
        MelonLogger.Msg("Restoring mod data");
        __instance._pod = modOptionsData.ModDataSetter(__instance._pod);
        _parsed = 0;
    }
}