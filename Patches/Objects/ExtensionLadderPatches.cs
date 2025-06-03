using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(ExtensionLadderItem))]
    internal class ExtensionLadderPatches
    {
        [HarmonyPatch(nameof(ExtensionLadderItem.StartLadderAnimation))]
        [HarmonyPostfix]
        static void ExtensionLadderItem_Post_StartLadderAnimation(ExtensionLadderItem __instance)
        {
            if (__instance.ladderBlinkWarning)
            {
                __instance.ladderBlinkWarning = false;
                Plugin.Logger.LogDebug("Fixed broken extension ladder warning");
            }
        }

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
    }
}
