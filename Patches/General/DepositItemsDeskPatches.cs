using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(DepositItemsDesk))]
    static class DepositItemsDeskPatches
    {
        [HarmonyPatch(nameof(DepositItemsDesk.Start))]
        [HarmonyPostfix]
        static void DepositItemsDesk_Post_Start(DepositItemsDesk __instance)
        {
            __instance.StartCoroutine(NonPatchFunctions.ResyncCompanyRandomOnDelay(__instance));
        }
    }
}
