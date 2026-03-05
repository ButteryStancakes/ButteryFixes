using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    internal class AIPatches
    {
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.SubtractFromPowerLevel))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        static void EnemyAI_Pre_SubtractFromPowerLevel(EnemyAI __instance, ref object[] __state)
        {
            __state =
            [
                __instance.removedPowerLevel,
                RoundManager.Instance.currentEnemyPower,
                RoundManager.Instance.currentOutsideEnemyPower,
                RoundManager.Instance.currentDaytimeEnemyPower
            ];
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.SubtractFromPowerLevel))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        static void EnemyAI_Post_SubtractFromPowerLevel(EnemyAI __instance, object[] __state)
        {
            if ((bool)__state[0] || !__instance.removedPowerLevel)
                return;

            if (__instance is ButlerEnemyAI butlerEnemyAI && Configuration.maskHornetsPower.Value)
            {
                float powerLevel = butlerEnemyAI.butlerBeesEnemyType.PowerLevel;
                if (powerLevel <= 0f || butlerEnemyAI.enemyType.PowerLevel <= 0f)
                    return;

                float currentEnemyPower = (float)__state[1];
                float currentOutsideEnemyPower = (float)__state[2];
                float currentDaytimeEnemyPower = (float)__state[3];

                if (RoundManager.Instance.currentEnemyPower < currentEnemyPower)
                {
                    Plugin.Logger.LogDebug("Butler died and subtracted inside power");

                    RoundManager.Instance.currentEnemyPower += powerLevel;
                    Plugin.Logger.LogDebug("Mask hornets added inside power");

                    if (!RoundManager.Instance.cannotSpawnMoreInsideEnemies)
                    {
                        if (RoundManager.Instance.currentEnemyPower >= RoundManager.Instance.currentMaxInsidePower)
                        {
                            RoundManager.Instance.cannotSpawnMoreInsideEnemies = true;
                            Plugin.Logger.LogDebug($"Mask hornets canceled vent spawns again ({RoundManager.Instance.currentEnemyPower} > {RoundManager.Instance.currentMaxInsidePower})");
                        }
                    }

                    return;
                }

                if (RoundManager.Instance.currentOutsideEnemyPower < currentOutsideEnemyPower)
                {
                    Plugin.Logger.LogDebug("Butler died and subtracted outside power");

                    RoundManager.Instance.currentOutsideEnemyPower += powerLevel;
                    Plugin.Logger.LogDebug("Mask hornets added outside power");

                    return;
                }

                if (RoundManager.Instance.currentDaytimeEnemyPower < currentDaytimeEnemyPower)
                {
                    Plugin.Logger.LogDebug("Butler died and subtracted daytime power");

                    RoundManager.Instance.currentDaytimeEnemyPower += powerLevel;
                    Plugin.Logger.LogDebug("Mask hornets added daytime power");

                    return;
                }

                Plugin.Logger.LogWarning("Butler died, unable to determine power type");
            }
        }

        [HarmonyPatch(typeof(DoorLock), nameof(DoorLock.OnTriggerStay))]
        [HarmonyPrefix]
        public static bool DoorLock_Pre_OnTriggerStay(Collider other)
        {
            // snare fleas, tulip snakes, and maneater don't open door when latching to player
            return !(other.CompareTag("Enemy") && other.TryGetComponent(out EnemyAICollisionDetect enemyAICollisionDetect) && (enemyAICollisionDetect.mainScript is CentipedeAI { clingingToPlayer: not null } || enemyAICollisionDetect.mainScript is FlowerSnakeEnemy { clingingToPlayer: not null } || enemyAICollisionDetect.mainScript is CaveDwellerAI { propScript.playerHeldBy: not null }));
        }
    }
}
