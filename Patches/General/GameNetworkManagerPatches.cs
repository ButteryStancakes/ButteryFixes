using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class GameNetworkManagerPatches
    {
        [HarmonyPatch(nameof(GameNetworkManager.SaveGame))]
        [HarmonyPostfix]
        static void GameNetworkManager_Post_SaveGame(GameNetworkManager __instance)
        {
            if (!__instance.isHostingGame || StartOfRound.Instance.isChallengeFile || !StartOfRound.Instance.inShipPhase)
                return;

            Terminal terminal = GlobalReferences.Terminal;
            if (terminal != null)
            {
                try
                {
                    ES3.Save("ButteryFixes_DeliveryVehicle", terminal.orderedVehicleFromTerminal, __instance.currentSaveFileName);
                    if (terminal.vehicleInDropship)
                    {
                        //ES3.DeleteKey("ButteryFixes_DeliveryItems", __instance.currentSaveFileName);
                        Plugin.Logger.LogInfo($"Dropship inventory saved (Vehicle)");
                    }
                    else
                    {
                        ES3.Save("ButteryFixes_DeliveryItems", terminal.orderedItemsFromTerminal, __instance.currentSaveFileName);
                        Plugin.Logger.LogInfo($"Dropship inventory saved ({terminal.numberOfItemsInDropship} items)");
                    }
                }
                catch (System.Exception e)
                {
                    Plugin.Logger.LogError($"An error occurred while trying to save dropship inventory to file \"{__instance.currentSaveFileName}\"");
                    Plugin.Logger.LogError(e);
                }
            }
        }

        [HarmonyPatch(nameof(GameNetworkManager.Disconnect))]
        [HarmonyPostfix]
        static void GameNetworkManager_Post_Disconnect()
        {
            GlobalReferences.allEnemiesList.Clear();
            GlobalReferences.lockingCamera = 0;
            GlobalReferences.sittingInArmchair = false;
            ButlerRadar.ClearAllButlers();
        }
    }
}
