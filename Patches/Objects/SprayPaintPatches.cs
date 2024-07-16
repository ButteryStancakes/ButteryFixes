using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    class SprayPaintPatches
    {
        [HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.LateUpdate))]
        [HarmonyPrefix]
        static void SprayPaintItemPreLateUpdate(SprayPaintItem __instance, ref bool ___isSpraying)
        {
            if (___isSpraying && __instance.itemProperties.canBeInspected && __instance.playerHeldBy != null && __instance.playerHeldBy.IsInspectingItem)
            {
                ___isSpraying = false;
                __instance.StopSpraying();
            }
        }

        [HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.ItemActivate))]
        [HarmonyPrefix]
        static void SprayPaintItemPreItemActivate(SprayPaintItem __instance, ref bool buttonDown, float ___sprayCanTank)
        {
            if (buttonDown && __instance.itemProperties.canBeInspected && __instance.playerHeldBy != null && __instance.playerHeldBy.IsInspectingItem && ___sprayCanTank > 0f)
                buttonDown = false;
        }

        [HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.DiscardItem))]
        [HarmonyPrefix]
        static void SprayPaintItemPreDiscardItem(SprayPaintItem __instance)
        {
            // vanilla calls this after base.DiscardItem() which means this reference will always be null
            if (__instance.playerHeldBy != null)
            {
                __instance.playerHeldBy.activatingItem = false;
                __instance.playerHeldBy.equippedUsableItemQE = false;
            }
        }
    }
}
