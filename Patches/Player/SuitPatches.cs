using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace ButteryFixes.Patches.Player
{
    [HarmonyPatch]
    internal class SuitPatches
    {
        static bool localCostumeChanged = false;

        [HarmonyPatch(typeof(UnlockableSuit), nameof(UnlockableSuit.ChangePlayerCostumeElement))]
        [HarmonyPrefix]
        static void UnlockableSuit_Pre_ChangePlayerCostumeElement(ref Transform costumeContainer, GameObject newCostume)
        {
            if (GameNetworkManager.Instance?.localPlayerController == null)
                return;

            if (costumeContainer == GameNetworkManager.Instance.localPlayerController.headCostumeContainerLocal)
            {
                // because of the return statement above, might require additional cleanup
                if (/*Compatibility.INSTALLED_MORE_COMPANY &&*/ costumeContainer.childCount > 0)
                {
                    foreach (Transform oldCostume in costumeContainer)
                        if (!oldCostume.CompareTag("DoNotSet"))
                            Object.Destroy(oldCostume.gameObject);
                }

                costumeContainer = GameNetworkManager.Instance.localPlayerController.headCostumeContainer;
                if (newCostume != null)
                    localCostumeChanged = true;
            }
            else if (costumeContainer == GameNetworkManager.Instance.localPlayerController.lowerTorsoCostumeContainer && newCostume != null)
                localCostumeChanged = true;
        }

        [HarmonyPatch(typeof(UnlockableSuit), nameof(UnlockableSuit.ChangePlayerCostumeElement))]
        [HarmonyPostfix]
        static void UnlockableSuit_Post_ChangePlayerCostumeElement(ref Transform costumeContainer)
        {
            if (localCostumeChanged)
            {
                localCostumeChanged = false;
                foreach (Renderer rend in costumeContainer.GetComponentsInChildren<Renderer>())
                    rend.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                Plugin.Logger.LogDebug($"Local costume part only draws shadow - {costumeContainer.name}");
            }
        }

        [HarmonyPatch(typeof(UnlockableSuit), nameof(UnlockableSuit.SwitchSuitForPlayer))]
        [HarmonyPostfix]
        static void UnlockableSuit_Post_SwitchSuitForPlayer(PlayerControllerB player, int suitID)
        {
            // to draw bunny tail in shadow
            if (GameNetworkManager.Instance.localPlayerController == player)
                UnlockableSuit.ChangePlayerCostumeElement(player.lowerTorsoCostumeContainer, StartOfRound.Instance.unlockablesList.unlockables[suitID].lowerTorsoCostumeObject);
        }
    }
}
