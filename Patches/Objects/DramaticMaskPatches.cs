using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class DramaticMaskPatches
    {
        [HarmonyPatch(typeof(HauntedMaskItem), nameof(HauntedMaskItem.MaskClampToHeadAnimationEvent))]
        [HarmonyPostfix]
        static void PostMaskClampToHeadAnimationEvent(HauntedMaskItem __instance)
        {
            if (__instance.maskTypeId == 5)
            {
                Plugin.Logger.LogInfo("Player is being converted by a Tragedy mask; about to replace mask prefab appearance");
                NonPatchFunctions.ConvertMaskToTragedy(__instance.currentHeadMask.transform);
            }
        }
    }
}
