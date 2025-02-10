using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    class ChargerCoilPatches
    {
        [HarmonyPatch(typeof(ItemCharger), nameof(ItemCharger.ChargeItem))]
        [HarmonyPostfix]
        static void ItemCharger_Post_ChargeItem(ItemCharger __instance)
        {
            if (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.requiresBattery && Configuration.lockInTerminal.Value)
                __instance.StartCoroutine(NonPatchFunctions.InteractionTemporarilyLocksCamera(__instance.triggerScript));
        }
    }
}
