using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine.Rendering;
using UnityEngine;

namespace ButteryFixes.Patches.Player
{
    [HarmonyPatch]
    internal class SuitPatches
    {
        static bool localCostumeChanged = false;

        [HarmonyPatch(typeof(UnlockableSuit), nameof(UnlockableSuit.ChangePlayerCostumeElement))]
        [HarmonyPrefix]
        static void PreChangePlayerCostumeElement(ref Transform costumeContainer, GameObject newCostume)
        {
            if (Compatibility.DISABLE_PLAYERMODEL_PATCHES)
                return;

            // MoreCompany changes player suits before the local player is initialized which would cause this function to throw an exception
            if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
                return;

            if (costumeContainer == GameNetworkManager.Instance.localPlayerController.headCostumeContainerLocal)
            {
                costumeContainer = GameNetworkManager.Instance.localPlayerController.headCostumeContainer;
                if (newCostume != null)
                    localCostumeChanged = true;
            }
            else if (costumeContainer == GameNetworkManager.Instance.localPlayerController.lowerTorsoCostumeContainer && newCostume != null)
                localCostumeChanged = true;
        }

        [HarmonyPatch(typeof(UnlockableSuit), nameof(UnlockableSuit.ChangePlayerCostumeElement))]
        [HarmonyPostfix]
        static void PostChangePlayerCostumeElement(ref Transform costumeContainer)
        {
            if (localCostumeChanged)
            {
                localCostumeChanged = false;
                foreach (Renderer rend in costumeContainer.GetComponentsInChildren<Renderer>())
                    rend.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                Plugin.Logger.LogInfo($"Local costume part only draws shadow - {costumeContainer.name}");
            }
        }

        [HarmonyPatch(typeof(UnlockableSuit), nameof(UnlockableSuit.SwitchSuitForPlayer))]
        [HarmonyPostfix]
        static void PostSwitchSuitForPlayer(PlayerControllerB player, int suitID)
        {
            // to draw bunny tail in shadow
            if (GameNetworkManager.Instance.localPlayerController == player)
                UnlockableSuit.ChangePlayerCostumeElement(player.lowerTorsoCostumeContainer, StartOfRound.Instance.unlockablesList.unlockables[suitID].lowerTorsoCostumeObject);
        }
    }
}
