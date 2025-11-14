using HarmonyLib;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(ShipBuildModeManager))]
    internal class ShipBuildModeManagerPatches
    {
        [HarmonyPatch(nameof(ShipBuildModeManager.StoreShipObjectClientRpc))]
        [HarmonyPostfix]
        static void ShipBuildModeManager_Post_StoreShipObjectClientRpc(ShipBuildModeManager __instance, int playerWhoStored)
        {
            // same tip, but this will force it to run for the host (who normally skips this code)
            if (__instance.IsServer && playerWhoStored == (int)GameNetworkManager.Instance.localPlayerController.playerClientId)
                HUDManager.Instance.DisplayTip("Item stored!", "You can see stored items in the terminal by using command 'STORAGE'", false, true, "LC_StorageTip");
        }
    }
}
