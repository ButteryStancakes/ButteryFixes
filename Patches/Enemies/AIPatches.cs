using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    internal class AIPatches
    {
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.EnableEnemyMesh))]
        [HarmonyPrefix]
        static void EnemyAIPreEnableEnemyMesh(EnemyAI __instance)
        {
            // minor optimization; this bug only really affects mimics
            if (Compatibility.DISABLE_ENEMY_MESH_PATCH || __instance is not MaskedPlayerEnemy)
                return;

            if (__instance.skinnedMeshRenderers.Length > 0)
            {
                for (int i = 0; i < __instance.skinnedMeshRenderers.Length; i++)
                {
                    if (__instance.skinnedMeshRenderers[i] == null)
                    {
                        __instance.skinnedMeshRenderers = __instance.skinnedMeshRenderers.Where(skinnedMeshRenderer => skinnedMeshRenderer != null).ToArray();
                        Plugin.Logger.LogWarning($"Removed all missing Skinned Mesh Renderers from enemy \"{__instance.name}\"");
                        break;
                    }
                }
            }
            if (__instance.meshRenderers.Length > 0)
            {
                for (int i = 0; i < __instance.meshRenderers.Length; i++)
                {
                    if (__instance.meshRenderers[i] == null)
                    {
                        __instance.meshRenderers = __instance.meshRenderers.Where(meshRenderer => meshRenderer != null).ToArray();
                        Plugin.Logger.LogWarning($"Removed all missing Mesh Renderers from enemy \"{__instance.name}\"");
                        break;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(EnemyAI), "SubtractFromPowerLevel")]
        [HarmonyPrefix]
        static void PreSubtractFromPowerLevel(EnemyAI __instance, ref bool ___removedPowerLevel)
        {
            if (!___removedPowerLevel)
            {
                MaskedPlayerEnemy maskedPlayerEnemy = __instance as MaskedPlayerEnemy;
                if (maskedPlayerEnemy != null)
                {
                    // should always work correctly for the host?
                    // I've only had mimickingPlayer desync on client
                    if (maskedPlayerEnemy.mimickingPlayer != null)
                    {
                        Plugin.Logger.LogInfo("\"Masked\" was mimicking a player; will not subtract from power level");
                        ___removedPowerLevel = true;
                    }
                }
                else if (__instance is ButlerEnemyAI)
                {
                    if (Configuration.maskHornetsPower.Value)
                    {
                        Plugin.Logger.LogInfo("Butler died, but mask hornets don't decrease power level");
                        ___removedPowerLevel = true;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(DoorLock), "OnTriggerStay")]
        [HarmonyPrefix]
        public static bool DoorLockPreOnTriggerStay(Collider other)
        {
            // snare fleas and tulip snakes don't open door when latching to player
            return !(other.CompareTag("Enemy") && other.TryGetComponent(out EnemyAICollisionDetect enemyAICollisionDetect) && (enemyAICollisionDetect.mainScript is CentipedeAI { clingingToPlayer: not null } || enemyAICollisionDetect.mainScript is FlowerSnakeEnemy { clingingToPlayer: not null }));
        }

        // prevents maneater "death" as a baby which has undefined behavior
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.KillEnemyOnOwnerClient))]
        [HarmonyPrefix]
        static bool PreKillEnemyOnOwnerClient(EnemyAI __instance, bool overrideDestroy)
        {
            if (__instance.IsOwner && __instance is CaveDwellerAI && __instance.currentBehaviourStateIndex == 0 && !overrideDestroy)
            {
                Plugin.Logger.LogInfo("Maneater was \"killed\" as a baby, but not destroyed. Kill will be cancelled");
                return false;
            }

            return true;
        }
    }
}
