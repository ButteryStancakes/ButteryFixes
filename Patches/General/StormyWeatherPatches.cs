using GameNetcodeStuff;
using HarmonyLib;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(StormyWeather))]
    class StormyWeatherPatches
    {
        [HarmonyPatch(nameof(StormyWeather.Update))]
        [HarmonyPostfix]
        static void StormyWeather_Post_Update(StormyWeather __instance)
        {
            if (RoundManager.Instance.IsOwner && __instance.targetingMetalObject != null && !__instance.hasShownStrikeWarning && StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(__instance.targetingMetalObject.transform.position) && (__instance.targetingMetalObject.playerHeldBy == null || (__instance.targetingMetalObject.playerHeldBy.isInHangarShipRoom && StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(__instance.targetingMetalObject.playerHeldBy.transform.position))))
            {
                Plugin.Logger.LogDebug($"Item {__instance.targetingMetalObject.name} #{__instance.targetingMetalObject.GetInstanceID()} was targeted by lightning but is inside the ship");
                PlayerControllerB player = __instance.targetingMetalObject.playerHeldBy ?? GameNetworkManager.Instance.localPlayerController;
                player.SetItemInElevator(true, true, __instance.targetingMetalObject);
                __instance.targetingMetalObject = null;
                // re-target sooner
                __instance.getObjectToTargetInterval = 3.86f; // every 0.14s
            }
        }
    }
}
