using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class ExtensionLadderPatches
    {
        [HarmonyPatch(typeof(ExtensionLadderItem), "StartLadderAnimation")]
        [HarmonyPostfix]
        static void PostStartLadderAnimation(ref bool ___ladderBlinkWarning)
        {
            if (___ladderBlinkWarning)
            {
                ___ladderBlinkWarning = false;
                Plugin.Logger.LogDebug("Fixed broken extension ladder warning");
            }
        }

        [HarmonyPatch(typeof(ExtensionLadderItem), nameof(ExtensionLadderItem.Update))]
        [HarmonyPostfix]
        static void ExtensionLadderItemPostUpdate(ExtensionLadderItem __instance)
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
