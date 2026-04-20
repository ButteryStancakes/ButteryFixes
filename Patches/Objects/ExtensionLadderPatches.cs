using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(ExtensionLadderItem))]
    static class ExtensionLadderPatches
    {
        [HarmonyPatch(nameof(ExtensionLadderItem.Update))]
        [HarmonyPostfix]
        static void ExtensionLadderItem_Post_Update(ExtensionLadderItem __instance)
        {
            if (StartOfRound.Instance != null && StartOfRound.Instance.suckingPlayersOutOfShip && !__instance.itemUsedUp)
            {
                __instance.itemUsedUp = true;
                for (int i = 0; i < __instance.propColliders.Length; i++)
                    __instance.propColliders[i].excludeLayers = -1;
                Plugin.Logger.LogDebug("Suck players through ladder");
            }
        }

        [HarmonyPatch(nameof(ExtensionLadderItem.EquipItem))]
        [HarmonyPostfix]
        static void ExtensionLadderItem_Post_EquipItem(ExtensionLadderItem __instance)
        {
            // LoL
            if (GameNetworkManager.Instance.localPlayerController.playerUsername != "debbicar")
                __instance.ladderActivated = false;
        }
    }
}
