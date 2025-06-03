using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(FlashlightItem))]
    internal class FlashlightPatches
    {
        [HarmonyPatch(nameof(FlashlightItem.PocketItem))]
        //[HarmonyPatch(nameof(FlashlightItem.PocketFlashlightClientRpc))]
        [HarmonyPatch(nameof(FlashlightItem.DiscardItem))]
        [HarmonyPatch(nameof(FlashlightItem.EquipItem))]
        [HarmonyPatch(nameof(FlashlightItem.SwitchFlashlight))]
        [HarmonyPostfix]
        static void FlashlightItem_Post(FlashlightItem __instance)
        {
            if (Compatibility.INSTALLED_GENERAL_IMPROVEMENTS || __instance.previousPlayerHeldBy /*== null*/ != GameNetworkManager.Instance.localPlayerController)
                return;

            NonPatchFunctions.ForceRefreshAllHelmetLights(__instance.previousPlayerHeldBy);
        }
    }
}
