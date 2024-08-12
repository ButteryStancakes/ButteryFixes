using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ButteryFixes.Utility
{
    static class NonPatchFunctions
    {
        internal static bool[] playerWasLastSprinting = new bool[/*4*/50];

        internal static IEnumerator ShellsAppearAfterDelay(ShotgunItem shotgun)
        {
            bool wasHeldByEnemy = shotgun.isHeldByEnemy;
            float timeSinceStart = Time.realtimeSinceStartup;

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

            if (!wasHeldByEnemy)
                yield return new WaitUntil(() => !shotgun.isReloading || Time.realtimeSinceStartup - timeSinceStart > 3f);

            // disables shells rendering when the gun is closed, to prevent bleedthrough with LOD1 model
            shotgun.shotgunShellLeft.forceRenderingOff = true;
            shotgun.shotgunShellRight.forceRenderingOff = true;
            Plugin.Logger.LogInfo($"Finished animating shotgun shells (held by enemy: {shotgun.isHeldByEnemy})");
        }

        public static void ShotgunPreProcess(Vector3 shotgunPosition, ref int num, ref RaycastHit[] results)
        {
            int index = 0;
            HashSet<EnemyAI> enemies = [];
            List<RaycastHit> invincibles = [];

            // sort in order of distance
            RaycastHit[] sortedResults = [.. results.Take(num).OrderBy(hit => Vector3.Distance(shotgunPosition, hit.point))];

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
                mesh.GetComponent<MeshRenderer>().sharedMaterial = GlobalReferences.tragedyMaskMat;

                MeshFilter tragedyMaskEyesFilled = mesh.Find("EyesFilled")?.GetComponent<MeshFilter>();
                if (tragedyMaskEyesFilled != null && GlobalReferences.tragedyMaskEyesFilled != null)
                {
                    tragedyMaskEyesFilled.mesh = GlobalReferences.tragedyMaskEyesFilled;

                    MeshFilter tragedyMaskLOD = mask.Find("ComedyMaskLOD1")?.GetComponent<MeshFilter>();
                    if (tragedyMaskLOD != null && GlobalReferences.tragedyMaskLOD != null)
                    {
                        tragedyMaskLOD.mesh = GlobalReferences.tragedyMaskLOD;
                        tragedyMaskLOD.GetComponent<MeshRenderer>().sharedMaterial = GlobalReferences.tragedyMaskMat;

                        Plugin.Logger.LogInfo("All mask meshes replaced successfully");
                    }
                    else
                        Plugin.Logger.LogWarning("Failed to replace mask eyes");
                }
                else
                    Plugin.Logger.LogWarning("Failed to replace mask LOD");
            }
            else
                Plugin.Logger.LogWarning("Failed to replace mask mesh");
        }

        internal static void ForceRefreshAllHelmetLights(PlayerControllerB player, bool forceOff = false)
        {
            for (int i = 0; i < player.allHelmetLights.Length; i++)
            {
                bool enable = false;
                if (!forceOff)
                {
                    for (int j = 0; j < player.ItemSlots.Length; j++)
                    {
                        if (player.ItemSlots[j] != null)
                        {
                            FlashlightItem flashlightItem = player.ItemSlots[j] as FlashlightItem;
                            if (flashlightItem != null && flashlightItem.flashlightTypeID == i && flashlightItem.isPocketed && flashlightItem.isBeingUsed && flashlightItem.insertedBattery.charge > 0f)
                            {
                                enable = true;
                                break;
                            }
                        }
                    }
                }
                if (player.allHelmetLights[i].enabled != enable)
                {
                    player.allHelmetLights[i].enabled = enable;
                    Plugin.Logger.LogInfo($"Fixed erroneous active state of {player.playerUsername}'s helmet light \"{player.allHelmetLights[i].name}\" (now {enable})");
                }
            }
        }

        public static void SpawnProbabilitiesPostProcess(ref List<int> spawnProbabilities, List<SpawnableEnemyWithRarity> enemies)
        {
            for (int i = 0; i < spawnProbabilities.Count; i++)
            {
                EnemyType enemyType = enemies[i].enemyType;
                // prevent old birds from eating up spawns when there are no dormant nests left
                if (enemyType.requireNestObjectsToSpawn && spawnProbabilities[i] > 0 && !Object.FindObjectsOfType<EnemyAINestSpawnObject>().Any(nest => nest.enemyType == enemyType))
                {
                    Plugin.Logger.LogDebug($"Enemy \"{enemyType.enemyName}\" spawning is disabled; no nests present on map");
                    spawnProbabilities[i] = 0;
                }
                // prevents spawn weight from exceeding "maximum"
                else if (Configuration.limitSpawnChance.Value && spawnProbabilities[i] > 100 && StartOfRound.Instance.currentLevelID < GlobalReferences.NUM_LEVELS)
                {
                    Plugin.Logger.LogDebug($"Enemy \"{enemyType.enemyName}\" is exceeding maximum spawn weight ({spawnProbabilities[i]} > 100)");
                    spawnProbabilities[i] = 100;
                }
            }
        }

        public static void OldBirdSpawnsFromApparatus()
        {
            if (Configuration.unlimitedOldBirds.Value)
                return;

            if (GlobalReferences.allEnemiesList.TryGetValue("RadMech", out EnemyType oldBird))
            {
                oldBird.numberSpawned++;
                RoundManager.Instance.currentOutsideEnemyPower += oldBird.PowerLevel;
                Plugin.Logger.LogInfo("Old Bird spawned from apparatus");
            }
        }

        internal static void SmokingHotCorpse(Transform body)
        {
            if (GlobalReferences.smokeParticle == null || !body.TryGetComponent(out SkinnedMeshRenderer mesh))
                return;

            foreach (Transform child in body)
                if (child.name.StartsWith(GlobalReferences.smokeParticle.name))
                    return;

            GameObject smokeParticle = Object.Instantiate(GlobalReferences.smokeParticle, body.transform);
            smokeParticle.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            smokeParticle.transform.localScale = Vector3.one;
            ParticleSystem.ShapeModule shape = smokeParticle.GetComponent<ParticleSystem>().shape;
            shape.skinnedMeshRenderer = mesh;
            Plugin.Logger.LogInfo("Smoke from freshly burnt corpse");
        }
    }
}
