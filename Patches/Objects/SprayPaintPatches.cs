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
    }
}
