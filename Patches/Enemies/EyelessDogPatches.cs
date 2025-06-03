using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(MouthDogAI))]
    internal class EyelessDogPatches
    {
        [HarmonyPatch(nameof(MouthDogAI.DetectNoise))]
        [HarmonyPostfix]
        static void MouthDogAI_Post_DetectNoise(MouthDogAI __instance)
        {
            // vanilla doesn't always clamp suspicion level to 11 (maxSuspicionLevel)
            if (__instance.suspicionLevel > 11)
                __instance.suspicionLevel = 11;
        }

        [HarmonyPatch(nameof(MouthDogAI.DetectNoise))]
        [HarmonyPrefix]
        static void MouthDogAI_Pre_OnCollideWithEnemy(MouthDogAI __instance)
        {
            // don't damage enemies while dead/stunned
            if (__instance.isEnemyDead)
                __instance.timeSinceHittingOtherEnemy = 0f;
            else if (__instance.stunNormalizedTimer > 0f)
                __instance.timeSinceHittingOtherEnemy = Mathf.Max(0f, 1f - (__instance.stunNormalizedTimer / __instance.enemyType.stunTimeMultiplier));
        }
    }
}
