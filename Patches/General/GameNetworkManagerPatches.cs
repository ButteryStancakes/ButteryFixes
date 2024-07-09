using ButteryFixes.Utility;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch]
    internal class GameNetworkManagerPatches
    {
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.SaveGame))]
        [HarmonyPostfix]
        static void PostSaveGame(GameNetworkManager __instance)
        {
            if (!__instance.isHostingGame || StartOfRound.Instance.isChallengeFile || !StartOfRound.Instance.inShipPhase)
                return;

            Terminal terminal = Object.FindObjectOfType<Terminal>();
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
                    Plugin.Logger.LogError($"An error occurred while trying to save dropship inventory to file \"{GameNetworkManager.Instance.currentSaveFileName}\"");
                    Plugin.Logger.LogError(e);
                }
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Disconnect))]
        [HarmonyPostfix]
        static void GameNetworkManagerPostDisconnect()
        {
            GlobalReferences.allEnemiesList.Clear();
        }
    }
}
