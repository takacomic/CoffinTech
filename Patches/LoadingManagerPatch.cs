using HarmonyLib;
using Il2CppVampireSurvivors.Data;
using Il2CppVampireSurvivors.Framework.DLC;
using Il2CppVampireSurvivors.Framework.Loading;
using UnityEngine;

namespace CoffinTech.Patches;

[HarmonyPatch(typeof(LoadingManager))]
static class LoadingManagerPatch
{
    [HarmonyPatch(nameof(LoadingManager.MountDlc))]
    [HarmonyPrefix]
    public static void MountDlc_Prefix(LoadingManager __instance, DlcType dlcType, Action callback)
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), DlcSystem.DlcCatalog._DlcData[dlcType]._Steam._AppID);
        AddressableLoader.SetInternalIdTransform();
        AddressableLoader.SetPath(path);
        if (!string.IsNullOrEmpty(path) && path != Directory.GetCurrentDirectory())
            __instance.MountedPaths.TryAdd(dlcType, path);
    }
    
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