using HarmonyLib;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(ItemDropship))]
    static class ItemDropshipPatches
    {
        [HarmonyPatch(nameof(ItemDropship.ShipLeave))]
        [HarmonyPostfix]
        static void ItemDropship_Post_ShipLeave(ItemDropship __instance)
        {
            if (!__instance.IsServer && __instance.triggerScript.interactable)
                HUDManager.Instance.DisplayTip("Items missed!", "The vehicle returned with your purchased items. Our delivery fee cannot be refunded.");
        }

        [HarmonyPatch(nameof(ItemDropship.Start))]
        [HarmonyPostfix]
        static void ItemDropship_Post_Start(ItemDropship __instance)
        {
            KillLocalPlayer killTrigger = __instance.GetComponentInChildren<KillLocalPlayer>();
            if (killTrigger != null)
            {
                killTrigger.causeOfDeath = CauseOfDeath.Crushing;
                Plugin.Logger.LogDebug("Cause of death: Dropship");
            }
        }
    }
}
