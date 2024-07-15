using ButteryFixes.Utility;
using GameNetcodeStuff;
using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class FlashlightPatches
    {
        [HarmonyPatch(typeof(FlashlightItem), nameof(FlashlightItem.PocketItem))]
        //[HarmonyPatch(typeof(FlashlightItem), nameof(FlashlightItem.PocketFlashlightClientRpc))]
        [HarmonyPatch(typeof(FlashlightItem), nameof(FlashlightItem.DiscardItem))]
        [HarmonyPatch(typeof(FlashlightItem), nameof(FlashlightItem.EquipItem))]
        [HarmonyPatch(typeof(FlashlightItem), nameof(FlashlightItem.SwitchFlashlight))]
        [HarmonyPostfix]
        static void FlashlightItemPost(PlayerControllerB ___previousPlayerHeldBy)
        {
            if (Compatibility.INSTALLED_GENERAL_IMPROVEMENTS || ___previousPlayerHeldBy /*== null*/ != GameNetworkManager.Instance.localPlayerController)
                return;

            NonPatchFunctions.ForceRefreshAllHelmetLights(___previousPlayerHeldBy);
        }
    }
}
