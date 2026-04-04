using ButteryFixes.Utility;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(StormyWeather))]
    static class StormyWeatherPatches
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

        [HarmonyPatch(nameof(StormyWeather.LightningStrike))]
        [HarmonyPostfix]
        static void StormyWeather_Post_LightningStrike(StormyWeather __instance, Vector3 strikePosition)
        {
            if (Vector3.Distance(strikePosition, __instance.explosionEffectParticle.transform.position) < 0.3f)
            {
                GlobalReferences.lastLightningStrike = __instance.explosionEffectParticle.transform.position;
                GlobalReferences.lightningLastStruck = Time.realtimeSinceStartup;
            }
        }

        [HarmonyPatch(nameof(StormyWeather.OnDisable))]
        [HarmonyPostfix]
        static void StormyWeather_Post_OnDisable(StormyWeather __instance)
        {
            GlobalReferences.lightningLastStruck = 0f;
            GlobalReferences.lastLightningStrike = new(3000f, 0f, 3000f);
        }

        [HarmonyPatch(nameof(StormyWeather.OnEnable))]
        [HarmonyPostfix]
        static void StormyWeather_Post_OnEnable(StormyWeather __instance)
        {
            ParticleSystemRenderer particleSystemRenderer = __instance.staticElectricityParticle.GetComponent<ParticleSystemRenderer>();
            if (particleSystemRenderer != null && particleSystemRenderer.renderMode == ParticleSystemRenderMode.VerticalBillboard)
            {
                particleSystemRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                particleSystemRenderer.alignment = ParticleSystemRenderSpace.View;
                Plugin.Logger.LogDebug("Stormy: Fix static billboarding");
            }
        }
    }
}
