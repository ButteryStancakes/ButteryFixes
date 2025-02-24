using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch]
    internal class RoundManagerPatches
    {
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.PredictAllOutsideEnemies))]
        [HarmonyPostfix]
        static void PostPredictAllOutsideEnemies()
        {
            // cleans up leftover spawn numbers from previous day + spawn predictions (when generating nests)
            foreach (string name in GlobalReferences.allEnemiesList.Keys)
                GlobalReferences.allEnemiesList[name].numberSpawned = 0;
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.OnDestroy))]
        [HarmonyPostfix]
        static void RoundManagerPostOnDestroy(RoundManager __instance)
        {
            // prevents persistence when quitting mid-day and rehosting
            __instance.ResetEnemyVariables();
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.PowerSwitchOffClientRpc))]
        [HarmonyPostfix]
        static void PostPowerSwitchOffClientRpc()
        {
            Object.FindAnyObjectByType<BreakerBox>()?.breakerBoxHum.Stop();
        }

        [HarmonyPatch(typeof(RoundManager), "SetExitIDs")]
        [HarmonyPostfix]
        static void PostSetExitIDs(RoundManager __instance)
        {
            if (Configuration.fixFireExits.Value)
            {
                foreach (EntranceTeleport entranceTeleport in Object.FindObjectsByType<EntranceTeleport>(FindObjectsSortMode.None))
                {
                    if (entranceTeleport.entranceId > 0 && !entranceTeleport.isEntranceToBuilding)
                    {
                        entranceTeleport.entrancePoint.localRotation = Quaternion.Euler(entranceTeleport.entrancePoint.localEulerAngles.x, entranceTeleport.entrancePoint.localEulerAngles.y + 180f, entranceTeleport.entrancePoint.localEulerAngles.z);
                        Plugin.Logger.LogInfo("Fixed rotation of internal fire exit");
                    }
                }
            }
        }

        static IEnumerable<CodeInstruction> TransSpawnRandomEnemy(List<CodeInstruction> codes, string firstTime, string enemies, string id)
        {
            FieldInfo firstTimeSpawning = AccessTools.Field(typeof(RoundManager), firstTime);
            for (int i = 2; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Stfld && (FieldInfo)codes[i].operand == firstTimeSpawning)
                {
                    codes.InsertRange(i - 2, [
                        new(OpCodes.Ldarg_0),
                        new(OpCodes.Ldflda, ReflectionCache.SPAWN_PROBABILITIES),
                        new(OpCodes.Ldarg_0),
                        new(OpCodes.Ldfld, ReflectionCache.CURRENT_LEVEL),
                        new(OpCodes.Ldfld, AccessTools.Field(typeof(SelectableLevel), enemies)),
                        new(OpCodes.Call, ReflectionCache.SPAWN_PROBABILITIES_POST_PROCESS)
                    ]);
                    Plugin.Logger.LogDebug($"Transpiler ({id}): Post process probabilities");
                    //i += 6;
                    return codes;
                }
            }

            Plugin.Logger.LogError($"{id} transpiler failed");
            return codes;
        }

        [HarmonyPatch(typeof(RoundManager), "AssignRandomEnemyToVent")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> TransAssignRandomEnemyToVent(IEnumerable<CodeInstruction> instructions)
        {
            return TransSpawnRandomEnemy(instructions.ToList(), "firstTimeSpawningEnemies", nameof(SelectableLevel.Enemies), "Spawner");
        }

        [HarmonyPatch(typeof(RoundManager), "SpawnRandomOutsideEnemy")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> TransSpawnRandomOutsideEnemy(IEnumerable<CodeInstruction> instructions)
        {
            return TransSpawnRandomEnemy(instructions.ToList(), "firstTimeSpawningOutsideEnemies", nameof(SelectableLevel.OutsideEnemies), "Outside spawner");
        }

        [HarmonyPatch(typeof(RoundManager), "SpawnRandomDaytimeEnemy")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> TransSpawnRandomDaytimeEnemy(IEnumerable<CodeInstruction> instructions)
        {
            return TransSpawnRandomEnemy(instructions.ToList(), "firstTimeSpawningDaytimeEnemies", nameof(SelectableLevel.DaytimeEnemies), "Daytime spawner");
        }

        [HarmonyPatch(typeof(RoundManager), "Awake")]
        [HarmonyPostfix]
        static void RoundManagerPostAwake(RoundManager __instance)
        {
            TileOverrides.OverrideTiles(__instance.dungeonFlowTypes);
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.RefreshLightsList))]
        [HarmonyPrefix]
        static void PreRefreshLightsList()
        {
            if (!Compatibility.INSTALLED_REBALANCED_MOONS && (StartOfRound.Instance.currentLevel.sceneName == "Level6Dine" || StartOfRound.Instance.currentLevel.sceneName == "Level9Artifice"))
            {
                foreach (GameObject poweredLight in GameObject.FindGameObjectsWithTag("PoweredLight"))
                {
                    if (poweredLight.name.StartsWith("NeonLights") && poweredLight.transform.position.y > -100f)
                    {
                        poweredLight.tag = "Untagged";
                        Plugin.Logger.LogDebug($"{StartOfRound.Instance.currentLevel.PlanetName}: Exterior lights disconnected from interior power");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RoundManager), "SpawnOutsideHazards")]
        [HarmonyPostfix]
        static void PostSpawnOutsideHazards(RoundManager __instance)
        {
            // this can't run in OnSceneLoaded because the navmesh needs to be baked first
            if (!Compatibility.INSTALLED_REBALANCED_MOONS && StartOfRound.Instance.currentLevel.sceneName == "Level6Dine")
            {
                foreach (string chainlinkFenceName in new string[]{
                    "ChainlinkFence",
                    "ChainlinkFence (1)",
                    "ChainlinkFence (2)",
                    "ChainlinkFence (3)",
                    "Collider",
                    "Collider (1)"
                })
                {
                    GameObject chainlinkFence = GameObject.Find("/Environment/Map/" + chainlinkFenceName);
                    if (chainlinkFence != null)
                    {
                        if (chainlinkFence.GetComponent<Renderer>() != null)
                        {
                            GameObject chainlinkFake = new(chainlinkFenceName + "Collider")
                            {
                                layer = 28
                            };
                            chainlinkFake.transform.SetParent(chainlinkFence.transform.parent);
                            chainlinkFake.transform.SetPositionAndRotation(chainlinkFence.transform.position, chainlinkFence.transform.rotation);
                            chainlinkFake.transform.localScale = chainlinkFence.transform.localScale;
                            foreach (BoxCollider boxCollider in chainlinkFence.GetComponents<BoxCollider>())
                            {
                                boxCollider.enabled = false;
                                BoxCollider chainlinkCollider = chainlinkFake.AddComponent<BoxCollider>();
                                chainlinkCollider.center = boxCollider.center;
                                chainlinkCollider.size = boxCollider.size;
                            }
                        }
                        else
                            chainlinkFence.layer = 28;
                    }
                }
                Plugin.Logger.LogDebug("Dine - Adjusted fences for enemy line-of-sight");
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.FinishGeneratingNewLevelClientRpc))]
        [HarmonyPostfix]
        static void PostFinishGeneratingNewLevelClientRpc(RoundManager __instance)
        {
            if (!__instance.IsServer || __instance.currentDungeonType != 4)
                return;

            MineshaftElevatorController mineshaftElevatorController = __instance.spawnedSyncedObjects.FirstOrDefault(spawnedSyncedObject => spawnedSyncedObject.name.StartsWith("MineshaftElevator"))?.GetComponent<MineshaftElevatorController>();
            if (mineshaftElevatorController == null)
            {
                Plugin.Logger.LogWarning("Mineshaft interior was selected, but could not find the elevator on host");
                return;
            }

            foreach (SpikeRoofTrap spikeRoofTrap in Object.FindObjectsByType<SpikeRoofTrap>(FindObjectsSortMode.None))
            {
                if (Vector3.Distance(spikeRoofTrap.spikeTrapAudio.transform.position, mineshaftElevatorController.elevatorBottomPoint.position) < 7f)
                {
                    NetworkObject netObj = spikeRoofTrap.GetComponentInParent<NetworkObject>();
                    if (netObj != null && netObj.IsSpawned)
                    {
                        Plugin.Logger.LogDebug($"Spike trap #{spikeRoofTrap.GetInstanceID()} was destroyed (too close to the elevator)");
                        netObj.Despawn();
                    }
                    else
                        Plugin.Logger.LogWarning("Error occurred while despawning spike trap (could not find network object, or it was not network spawned yet)");
                }
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.RefreshEnemiesList))]
        [HarmonyPostfix]
        static void RoundManager_Post_RefreshEnemiesList(RoundManager __instance)
        {
            if (StartOfRound.Instance.isChallengeFile && Configuration.limitSpawnChance.Value)
                __instance.enemyRushIndex = -1;
        }
    }
}
