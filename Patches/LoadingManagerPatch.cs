using HarmonyLib;
using Il2CppVampireSurvivors.Data;
using Il2CppVampireSurvivors.Framework.DLC;
using Il2CppVampireSurvivors.Framework.Loading;
using UnityEngine;

namespace CoffinTech.Patches;

[HarmonyPatch(typeof(LoadingManager))]
static class LoadingManagerPatch
{
    [HarmonyPatch(nameof(LoadingManager.LoadDlcs))]
    [HarmonyPrefix]
    public static void PreLoadDlcs(LoadingManager __instance)
    {
        foreach (var dlc in DlcPatches._dlcTypes)
        {
            DlcData data = ScriptableObject.CreateInstance<DlcData>();
            data._DlcType = dlc.Key;
            data._Title = dlc.Value[0] as string;
            data._ContentGroupType = ContentGroupType.EXTRA; 
            data._ExpectedVersion = dlc.Value[1] as string;
            data._HasBeenReleased = true;
            DlcSystem.DlcCatalog._DlcData.TryAdd(dlc.Key, data);
            DlcSystem.SelectedDlc.TryAdd(dlc.Key, true);
        }
    }
}