﻿using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ButteryFixes.Utility
{
    static class NonPatchFunctions
    {
        internal static bool[] playerWasLastSprinting = new bool[/*4*/50];

        public static void ShotgunPreProcess(Vector3 shotgunPosition, ref int num, ref RaycastHit[] results)
        {
            int index = 0;
            HashSet<EnemyAI> enemies = new();
            List<RaycastHit> invincibles = new();

            // sort in order of distance
            RaycastHit[] sortedResults = results.Take(num).OrderBy(hit => Vector3.Distance(shotgunPosition, hit.point)).ToArray();

            // remove all duplicates
            for (int i = 0; i < num; i++)
            {
                if (sortedResults[i].transform.TryGetComponent(out EnemyAICollisionDetect enemyCollider) && !enemyCollider.onlyCollideWhenGrounded)
                {
                    EnemyAI enemy = enemyCollider.mainScript;
                    if (enemies.Add(enemy))
                    {
                        EnemyType enemyType = enemy.enemyType;
                        // invincible enemies are low-priority
                        if (!enemyType.canDie || enemyType.name == "RadMech" || enemyType.name == "DocileLocustBees")
                            invincibles.Add(sortedResults[i]);
                        else if (!enemy.isEnemyDead)
                        {
                            results[index] = sortedResults[i];
                            index++;
                            // only hit 10 targets max
                            if (index == 10)
                            {
                                num = 10;
                                return;
                            }
                        }
                    }
                }
            }

            // add invincible enemies at the end, if there are slots leftover
            if (invincibles.Count > 0)
            {
                // slime is "medium priority" since they get angry when shot
                foreach (RaycastHit invincible in invincibles.OrderByDescending(invincible => invincible.transform.GetComponent<EnemyAICollisionDetect>().mainScript is BlobAI))
                {
                    results[index] = invincible;
                    index++;
                    if (index == 10)
                    {
                        num = 10;
                        return;
                    }
                }
            }

            num = index;
        }

        internal static IEnumerator ShellsAppearAfterDelay(ShotgunItem shotgun)
        {
            yield return new WaitForSeconds(shotgun.isHeldByEnemy ? 0.85f : 1.9f);

            if (shotgun.isHeldByEnemy)
            {
                // nutcrackers don't set isReloading
                shotgun.shotgunShellLeft.forceRenderingOff = false;
                shotgun.shotgunShellLeft.enabled = true;
                shotgun.shotgunShellRight.forceRenderingOff = false;
                shotgun.shotgunShellRight.enabled = true;
            }
            else if (shotgun.shotgunShellLeft.enabled)
                shotgun.shotgunShellRight.enabled = true;
            else
                shotgun.shotgunShellLeft.enabled = true;

            yield return new WaitForSeconds(shotgun.isHeldByEnemy ? 0.66f : 0.75f);

            if (!shotgun.isHeldByEnemy)
                yield return new WaitUntil(() => !shotgun.isReloading);

            // disables shells rendering when the gun is closed, to prevent bleedthrough with LOD1 model
            shotgun.shotgunShellLeft.forceRenderingOff = true;
            shotgun.shotgunShellRight.forceRenderingOff = true;
            Plugin.Logger.LogInfo($"Finished animating shotgun shells (held by enemy: {shotgun.isHeldByEnemy})");
        }

        internal static void FakeFootstepAlert(PlayerControllerB player)
        {
            bool noiseIsInsideClosedShip = player.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed;
            if (player.IsOwner ? player.isSprinting : playerWasLastSprinting[player.actualClientId])
                RoundManager.Instance.PlayAudibleNoise(player.transform.position, 22f, 0.6f, 0, noiseIsInsideClosedShip, 3322);
            else
                RoundManager.Instance.PlayAudibleNoise(player.transform.position, 17f, 0.4f, 0, noiseIsInsideClosedShip, 3322);
        }

        internal static void ConvertMaskToTragedy(Transform mask)
        {
            Transform mesh = mask.Find("Mesh");
            if (mesh != null && GlobalReferences.tragedyMask != null && GlobalReferences.tragedyMaskMat != null)
            {
                mesh.GetComponent<MeshFilter>().mesh = GlobalReferences.tragedyMask;
                mesh.GetComponent<MeshRenderer>().material = GlobalReferences.tragedyMaskMat;

                MeshFilter tragedyMaskEyesFilled = mesh.Find("EyesFilled")?.GetComponent<MeshFilter>();
                if (tragedyMaskEyesFilled != null && GlobalReferences.tragedyMaskEyesFilled != null)
                {
                    tragedyMaskEyesFilled.mesh = GlobalReferences.tragedyMaskEyesFilled;

                    MeshFilter tragedyMaskLOD = mask.Find("ComedyMaskLOD1")?.GetComponent<MeshFilter>();
                    if (tragedyMaskLOD != null && GlobalReferences.tragedyMaskLOD != null)
                    {
                        tragedyMaskLOD.mesh = GlobalReferences.tragedyMaskLOD;
                        tragedyMaskLOD.GetComponent<MeshRenderer>().material = GlobalReferences.tragedyMaskMat;

                        Plugin.Logger.LogInfo("All mask attachment meshes replaced successfully");
                    }
                    else
                        Plugin.Logger.LogWarning("Failed to replace mask attachment eyes");
                }
                else
                    Plugin.Logger.LogWarning("Failed to replace mask attachment LOD");
            }
            else
                Plugin.Logger.LogWarning("Failed to replace mask attachment mesh");
        }
    }
}
