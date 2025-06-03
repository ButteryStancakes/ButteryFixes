using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    internal class AIPatches
    {
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.SubtractFromPowerLevel))]
        [HarmonyPrefix]
        static void EnemyAI_Pre_SubtractFromPowerLevel(EnemyAI __instance)
        {
            if (__instance.removedPowerLevel)
                return;

            if (__instance is ButlerEnemyAI && Configuration.maskHornetsPower.Value)
            {
                Plugin.Logger.LogDebug("Butler died, but mask hornets don't decrease power level");
                __instance.removedPowerLevel = true;
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
