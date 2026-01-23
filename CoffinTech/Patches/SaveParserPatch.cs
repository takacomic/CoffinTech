using CoffinTech.SaveData;
using HarmonyLib;
using Il2CppVampireSurvivors.Framework.Saves;

namespace CoffinTech.Patches;

[HarmonyPatch(typeof(SaveParser))]
static class SaveParserPatch
{
    [HarmonyPatch(nameof(SaveParser.PostParseFixes))]
    [HarmonyPrefix]
    public static void PostParseFixes(SaveParser __instance)
    {
        ModOptionsData modOptionsData = new();
        __instance._pod = modOptionsData.ModDataSetter(__instance._pod);
    }
}