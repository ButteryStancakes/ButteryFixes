using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch]
    internal class FoliageDetailDistancePatches
    {
        [HarmonyPatch(typeof(FoliageDetailDistance), nameof(FoliageDetailDistance.Start))]
        [HarmonyPostfix]
        static void FoliageDetailDistance_Post_Start(FoliageDetailDistance __instance)
        {
            if (StartOfRound.Instance.currentLevelID < GlobalReferences.NUM_LEVELS)
            {
                __instance.lowDetailMaterial.color = __instance.highDetailMaterial.color;
                Plugin.Logger.LogDebug("Fix foliage colors");
            }
        }
    }
}
