using HarmonyLib;
using Il2CppVampireSurvivors.Data;
using Il2CppVampireSurvivors.Framework.DLC;
using Il2CppVampireSurvivors.Framework.Loading;

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
}