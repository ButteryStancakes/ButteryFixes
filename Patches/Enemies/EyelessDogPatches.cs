using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    internal class EyelessDogPatches
    {
        [HarmonyPatch(typeof(MouthDogAI), nameof(MouthDogAI.OnCollideWithPlayer))]
        [HarmonyPrefix]
        static bool MouthDogAIPreOnCollideWithPlayer(MouthDogAI __instance, Collider other)
        {
            if (__instance.isEnemyDead || (other.TryGetComponent(out PlayerControllerB player) && player.isInHangarShipRoom && (!__instance.isInsidePlayerShip || (StartOfRound.Instance.hangarDoorsClosed && !StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(__instance.transform.position)))))
                return false;

            return true;
        }

        [HarmonyPatch(typeof(MouthDogAI), nameof(MouthDogAI.DetectNoise))]
        [HarmonyPostfix]
        static void MouthDogAIPostDetectNoise(MouthDogAI __instance)
        {
            // vanilla doesn't always clamp suspicion level to 11 (maxSuspicionLevel)
            if (__instance.suspicionLevel > 11)
                __instance.suspicionLevel = 11;
        }

        [HarmonyPatch(typeof(MouthDogAI), nameof(MouthDogAI.DetectNoise))]
        [HarmonyPrefix]
        static void MouthDogAIPreOnCollideWithEnemy(MouthDogAI __instance, ref float ___timeSinceHittingOtherEnemy)
        {
            // don't damage enemies while dead/stunned
            if (__instance.isEnemyDead)
                ___timeSinceHittingOtherEnemy = 0f;
            else if (__instance.stunNormalizedTimer > 0f)
                ___timeSinceHittingOtherEnemy = Mathf.Max(0f, 1f - (__instance.stunNormalizedTimer / __instance.enemyType.stunTimeMultiplier));
        }
    }
}
