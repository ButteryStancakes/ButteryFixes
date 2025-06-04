using ButteryFixes.Utility;
using DunGen;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatches
    {
        [HarmonyPatch(nameof(RoundManager.PredictAllOutsideEnemies))]
        [HarmonyPostfix]
        static void RoundManager_Post_PredictAllOutsideEnemies()
        {
            // cleans up leftover spawn numbers from previous day + spawn predictions (when generating nests)
            foreach (string name in GlobalReferences.allEnemiesList.Keys)
                GlobalReferences.allEnemiesList[name].numberSpawned = 0;
        }

        [HarmonyPatch(nameof(RoundManager.OnDestroy))]
        [HarmonyPostfix]
        static void RoundManager_Post_OnDestroy(RoundManager __instance)
        {
            // prevents persistence when quitting mid-day and rehosting
            __instance.ResetEnemyVariables();
        }

        [HarmonyPatch(nameof(RoundManager.PowerSwitchOffClientRpc))]
        [HarmonyPostfix]
        static void RoundManager_Post_PowerSwitchOffClientRpc()
        {
            Object.FindAnyObjectByType<BreakerBox>()?.breakerBoxHum.Stop();
        }

        [HarmonyPatch(nameof(RoundManager.SetExitIDs))]
        [HarmonyPostfix]
        static void RoundManager_Post_SetExitIDs(RoundManager __instance)
        {
            EntranceTeleport[] entranceTeleports = Object.FindObjectsByType<EntranceTeleport>(FindObjectsSortMode.None);
            if (Configuration.fixFireExits.Value)
            {
                foreach (EntranceTeleport entranceTeleport in entranceTeleports)
                {
                    if (entranceTeleport.entranceId > 0 && !entranceTeleport.isEntranceToBuilding)
                    {
                        entranceTeleport.entrancePoint.localRotation = Quaternion.Euler(entranceTeleport.entrancePoint.localEulerAngles.x, entranceTeleport.entrancePoint.localEulerAngles.y + 180f, entranceTeleport.entrancePoint.localEulerAngles.z);
                        Plugin.Logger.LogDebug("Fixed rotation of internal fire exit");
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

        [HarmonyPatch(nameof(RoundManager.AssignRandomEnemyToVent))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> RoundManager_Trans_AssignRandomEnemyToVent(IEnumerable<CodeInstruction> instructions)
        {
            return TransSpawnRandomEnemy(instructions.ToList(), nameof(RoundManager.firstTimeSpawningEnemies), nameof(SelectableLevel.Enemies), "Spawner");
        }

        [HarmonyPatch(nameof(RoundManager.SpawnRandomOutsideEnemy))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> RoundManager_Trans_SpawnRandomOutsideEnemy(IEnumerable<CodeInstruction> instructions)
        {
            return TransSpawnRandomEnemy(instructions.ToList(), nameof(RoundManager.firstTimeSpawningOutsideEnemies), nameof(SelectableLevel.OutsideEnemies), "Outside spawner");
        }

        [HarmonyPatch(nameof(RoundManager.SpawnRandomDaytimeEnemy))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> RoundManager_Trans_SpawnRandomDaytimeEnemy(IEnumerable<CodeInstruction> instructions)
        {
            return TransSpawnRandomEnemy(instructions.ToList(), nameof(RoundManager.firstTimeSpawningDaytimeEnemies), nameof(SelectableLevel.DaytimeEnemies), "Daytime spawner");
        }

        [HarmonyPatch(nameof(RoundManager.Awake))]
        [HarmonyPostfix]
        static void RoundManager_Post_Awake(RoundManager __instance)
        {
            TileOverrides.OverrideTiles(__instance.dungeonFlowTypes);
        }

        [HarmonyPatch(nameof(RoundManager.RefreshLightsList))]
        [HarmonyPrefix]
        static void RoundManager_Pre_RefreshLightsList()
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

        [HarmonyPatch(nameof(RoundManager.SpawnOutsideHazards))]
        [HarmonyPostfix]
        static void RoundManager_Post_SpawnOutsideHazards(RoundManager __instance)
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

        [HarmonyPatch(nameof(RoundManager.FinishGeneratingNewLevelClientRpc))]
        [HarmonyPostfix]
        static void RoundManager_Post_FinishGeneratingNewLevelClientRpc(RoundManager __instance)
        {
            if (Configuration.disableLODFade.Value)
            {
                foreach (LODGroup lodGroup in Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None))
                {
                    if (lodGroup.fadeMode != LODFadeMode.None)
                    {
                        lodGroup.fadeMode = LODFadeMode.None;
                        Plugin.Logger.LogDebug($"Disable LOD fade on \"{lodGroup.name}\"");
                    }
                }
            }

            NonPatchFunctions.TestForVainShrouds();

            EntranceTeleport mainEntrance = Object.FindObjectsByType<EntranceTeleport>(FindObjectsSortMode.None).FirstOrDefault(teleport => teleport.entranceId == 0 && !teleport.isEntranceToBuilding);
            GlobalReferences.mainEntrancePos = mainEntrance != null ? mainEntrance.entrancePoint.position : Vector3.zero;

            GlobalReferences.caveTiles.Clear();
            if (__instance.currentDungeonType == 4)
            {
                GameObject dungeonRoot = __instance.dungeonGenerator?.Root ?? GameObject.Find("/Systems/LevelGeneration/LevelGenerationRoot");
                if (dungeonRoot == null)
                {
                    if (StartOfRound.Instance.currentLevel.name != "CompanyBuildingLevel")
                        Plugin.Logger.LogWarning("Landed on a moon with no dungeon generated. This shouldn't happen");

                    return;
                }

                foreach (Tile tile in dungeonRoot.GetComponentsInChildren<Tile>())
                {
                    if (tile.name.StartsWith("Cave"))
                    {
                        GlobalReferences.caveTiles.Add(tile.OverrideAutomaticTileBounds ? tile.transform.TransformBounds(tile.TileBoundsOverride) : tile.Bounds);
                        Plugin.Logger.LogDebug($"Cached bounds of tile {tile.name}");
                    }
                    else if (tile.name.StartsWith("MineshaftStartTile"))
                    {
                        // reused and adapted from Mask Fixes
                        // https://github.com/ButteryStancakes/MaskFixes/blob/165601d81827d368c119f12ced2bf8fdaffbeec8/Patches.cs#L621

                        // calculate the bounds of the elevator start room
                        // center:  ( -1,   51.37,  3.2 )
                        // size:    ( 30,      20,   15 )
                        Vector3[] corners =
                        [
                            new(-16f, 41.37f, -4.3f),
                            new(14f, 41.37f, -4.3f),
                            new(-16f, 61.37f, -4.3f),
                            new(14f, 61.37f, -4.3f),
                            new(-16f, 41.37f, 10.7f),
                            new(14f, 41.37f, 10.7f),
                            new(-16f, 61.37f, 10.7f),
                            new(14f, 61.37f, 10.7f),
                        ];
                        tile.transform.TransformPoints(corners);

                        // thanks Zaggy
                        Vector3 min = corners[0], max = corners[0];
                        for (int i = 1; i < corners.Length; i++)
                        {
                            min = Vector3.Min(min, corners[i]);
                            max = Vector3.Max(max, corners[i]);
                        }

                        GlobalReferences.mineStartBounds = new()
                        {
                            min = min,
                            max = max
                        };
                        Plugin.Logger.LogDebug("Calculated bounds for mineshaft elevator's start room");
                    }
                }
            }

            if (!__instance.IsServer || __instance.currentDungeonType != 4)
                return;

            MineshaftElevatorController mineshaftElevatorController = RoundManager.Instance.currentMineshaftElevator ?? __instance.spawnedSyncedObjects.FirstOrDefault(spawnedSyncedObject => spawnedSyncedObject.name.StartsWith("MineshaftElevator"))?.GetComponent<MineshaftElevatorController>();
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

        [HarmonyPatch(nameof(RoundManager.SpawnScrapInLevel))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> RoundManager_Trans_SpawnScrapInLevel(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo itemId = AccessTools.Field(typeof(Item), nameof(Item.itemId));
            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld && (FieldInfo)codes[i].operand == itemId && codes[i + 1].opcode == OpCodes.Ldc_I4 && (int)codes[i + 1].operand == 152767)
                {
                    /*codes[i].opcode = OpCodes.Call;
                    codes[i].operand = AccessTools.DeclaredPropertyGetter(typeof(Object), nameof(Object.name));
                    codes[i + 1].opcode = OpCodes.Ldstr;
                    codes[i + 1].operand = "Zeddog";*/
                    codes[i + 1].operand = GlobalReferences.ZED_DOG_ID;
                    Plugin.Logger.LogDebug("Transpiler (Scrap spawn): Boost Zed Dog instead of gift box");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Scrap spawn transpiler failed");
            return instructions;
        }
    }
}
