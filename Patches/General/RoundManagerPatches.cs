using ButteryFixes.Utility;
using DunGen;
using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(RoundManager))]
    static class RoundManagerPatches
    {
        [HarmonyPatch(nameof(RoundManager.PredictAllOutsideEnemies))]
        [HarmonyPostfix]
        static void RoundManager_Post_PredictAllOutsideEnemies()
        {
            // cleans up leftover spawn numbers from previous day + spawn predictions (when generating nests)
            foreach (string name in GlobalReferences.allEnemiesList.Keys)
            {
                GlobalReferences.allEnemiesList[name].numberSpawned = 0;
                GlobalReferences.allEnemiesList[name].hasSpawnedAtLeastOne = false;
            }
        }

        [HarmonyPatch(nameof(RoundManager.PowerSwitchOffClientRpc))]
        [HarmonyPostfix]
        static void RoundManager_Post_PowerSwitchOffClientRpc()
        {
            Object.FindAnyObjectByType<BreakerBox>()?.breakerBoxHum.Stop();
        }

        [HarmonyPatch(nameof(RoundManager.Awake))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
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
        static void RoundManager_Post_SpawnOutsideHazards()
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
        }

        [HarmonyPatch(nameof(RoundManager.DespawnPropsAtEndOfRound))]
        [HarmonyPrefix]
        static void RoundManager_Pre_DespawnPropsAtEndOfRound(RoundManager __instance)
        {
            if (__instance.IsServer)
                return;

            // fix collected notification re-playing
            foreach (GrabbableObject shipScrap in __instance.scrapDroppedInShip)
            {
                if (shipScrap != null && shipScrap.isInShipRoom)
                    shipScrap.scrapPersistedThroughRounds = true;
            }
        }
    }
}
