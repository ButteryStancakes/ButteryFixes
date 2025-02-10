using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ButteryFixes.Utility
{
    public static class NonPatchFunctions
    {
        internal static bool[] playerWasLastSprinting = new bool[/*4*/50];
        internal static int hives;

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
            Plugin.Logger.LogDebug($"Finished animating shotgun shells (held by enemy: {shotgun.isHeldByEnemy})");
        }

        internal static void FakeFootstepAlert(PlayerControllerB player)
        {
            bool noiseIsInsideClosedShip = player.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed;
            if ((int)player.actualClientId < playerWasLastSprinting.Length && (player.IsOwner ? player.isSprinting : playerWasLastSprinting[(int)player.actualClientId]))
                RoundManager.Instance.PlayAudibleNoise(player.transform.position, 22f, 0.6f, 0, noiseIsInsideClosedShip, 3322);
            else
                RoundManager.Instance.PlayAudibleNoise(player.transform.position, 17f, 0.4f, 0, noiseIsInsideClosedShip, 3322);
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
                    Plugin.Logger.LogDebug($"Fixed erroneous active state of {player.playerUsername}'s helmet light \"{player.allHelmetLights[i].name}\" (now {enable})");
                }
            }
        }

        public static void SpawnProbabilitiesPostProcess(ref List<int> spawnProbabilities, List<SpawnableEnemyWithRarity> enemies)
        {
            if (spawnProbabilities.Count != enemies.Count)
                Plugin.Logger.LogWarning("SpawnProbabilities is a different size from the current enemies list. This should never happen outside of mod conflicts!");

            for (int i = 0; i < spawnProbabilities.Count && i < enemies.Count; i++)
            {
                EnemyType enemyType = enemies[i].enemyType;
                // prevent old birds from eating up spawns when there are no dormant nests left
                if (enemyType.requireNestObjectsToSpawn && spawnProbabilities[i] > 0 && !Object.FindObjectsByType<EnemyAINestSpawnObject>(FindObjectsSortMode.None).Any(nest => nest.enemyType == enemyType))
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
                Plugin.Logger.LogDebug("Old Bird spawned from apparatus");
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
            Plugin.Logger.LogDebug("Smoke from freshly burnt corpse");
        }

        public static void BabyEatsScrap(GrabbableObject grabObj)
        {
            if (grabObj.itemProperties.isScrap)
                GlobalReferences.scrapEaten += grabObj.scrapValue;
        }

        public static int RerollHivePrice(int price, Vector3 pos)
        {
            hives++;

            if (!Configuration.fixHivePrices.Value || StartOfRound.Instance.isChallengeFile)
                return price;

            System.Random random = new(StartOfRound.Instance.randomMapSeed + 1314 + hives);

            int temp = random.Next(50, 150);
            if (Vector3.Distance(pos, new(1.27146339f, 0.278438568f, -7.5f)) < 40f)
                temp = random.Next(40, 100);

            Plugin.Logger.LogDebug($"Hive price recalculated: ${price} -> ${temp}");
            return temp;
        }

        internal static IEnumerator InteractionTemporarilyLocksCamera(InteractTrigger trigger)
        {
            if (trigger == null)
                yield break;

            float startTime = Time.realtimeSinceStartup;

            while (trigger.playerScriptInSpecialAnimation != GameNetworkManager.Instance.localPlayerController && Time.realtimeSinceStartup - startTime < 2f)
                yield return null;

            if (trigger.playerScriptInSpecialAnimation != GameNetworkManager.Instance.localPlayerController)
            {
                Plugin.Logger.LogDebug("Failed to lock animation for local player, who never entered animation state");
                yield break;
            }

            GlobalReferences.lockingCamera++;

            yield return new WaitForSeconds(trigger.animationWaitTime);

            if (GlobalReferences.lockingCamera > 0)
                GlobalReferences.lockingCamera--;
        }
    }
}
