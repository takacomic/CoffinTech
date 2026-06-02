using System.Text.RegularExpressions;
using CoffinTech.Logger;
using CoffinTech.SaveData;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppVampireSurvivors.Data;
using Il2CppVampireSurvivors.Framework.Saves;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CoffinTech.Patches;


[HarmonyPatch(typeof(SaveUtils))]
static class SaveUtilsPatch
{

    [HarmonyPatch(nameof(SaveUtils.GetSerializedPlayerData))]
    [HarmonyPrefix]
    public static bool GetSerializedPlayerData(PlayerOptionsData data, ref Il2CppStructArray<byte> __result)
    {
        // Null check prevents NRE during game states where data might be uninitialized
        if (data == null)
        {
            __result = new Il2CppStructArray<byte>(0);
            return false;
        }
        
        ModOptionsData modOptionsData = new();
        __result = SaveUtils.JsonToBytes(SaveUtils.UpdateChecksum(SaveSerializer.Serialize(modOptionsData.ModDataRemover(data))));
        return  false;
    }

    [HarmonyPatch(nameof(SaveUtils.ChecksumIsValid))]
    [HarmonyPrefix]
    public static bool ChecksumIsValid(ref string rawData, ref string checksum, ref bool __result)
    {
        if (!CoffinTechMod.BypassChecksum) return true;
            
        __result = true;
        return false;
        
    }
}