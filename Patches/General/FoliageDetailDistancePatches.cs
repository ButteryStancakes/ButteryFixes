using ButteryFixes.Utility;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(FoliageDetailDistance))]
    static class FoliageDetailDistancePatches
    {
        static Material lowDetailMaterial;

        [HarmonyPatch(nameof(FoliageDetailDistance.Start))]
        [HarmonyPostfix]
        static void FoliageDetailDistance_Post_Start(FoliageDetailDistance __instance)
        {
            if (StartOfRound.Instance.currentLevelID < GlobalReferences.NUM_LEVELS)
            {
                if (lowDetailMaterial == null)
                {
                    lowDetailMaterial = Object.Instantiate(__instance.lowDetailMaterial);
                    lowDetailMaterial.color = __instance.highDetailMaterial.color;
                }
                __instance.lowDetailMaterial = lowDetailMaterial;
                Plugin.Logger.LogDebug("Fix foliage colors");
            }
        }
    }
}
