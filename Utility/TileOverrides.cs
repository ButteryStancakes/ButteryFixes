using BepInEx.Bootstrap;
using DunGen;
using DunGen.Graph;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace ButteryFixes.Utility
{
    internal static class TileOverrides
    {
        static readonly Vector3 MINESHAFT_FIRE_EXIT_SCALE = new(1.176471f, 1.176471f, 1.176471f);

        internal static void OverrideTiles(IndoorMapType[] indoorMapTypes)
        {
            Transform alleyExitDoorContainer = null, fireExitLight = null, breakerBoxSpawn = null, ventSpawn = null;
            bool removeAnomalySpawns = false, fixVentsSAndNW = false;
            foreach (GameObject tile in GetAllTiles(indoorMapTypes))
            {
                switch (tile.name)
                {
                    // --- FACTORY ---
                    // - fix items/traps spawning inside the piping on the stair tiles

                    // apparatus rooms
                    case "DoubleDoorRoom":
                        removeAnomalySpawns = true;
                        break;
                    case "MediumRoomHallway1B":
                        removeAnomalySpawns = true;
                        ventSpawn = tile.transform.Find("Vents (2)/Vent (1)");
                        if (ventSpawn != null)
                        {
                            ventSpawn.localPosition = new(-13.095f, ventSpawn.localPosition.y, ventSpawn.localPosition.z);
                            Plugin.Logger.LogDebug($"{tile.name}: Corrected position for \"{ventSpawn.name}\"");
                        }
                        break;

                    // locker room
                    case "4x4BigStairTile":
                        foreach (RandomScrapSpawn randomScrapSpawn in tile.GetComponentsInChildren<RandomScrapSpawn>())
                        {
                            if (randomScrapSpawn.name.StartsWith("SmallItemsSpawn"))
                            {
                                randomScrapSpawn.gameObject.SetActive(false);
                                Plugin.Logger.LogDebug($"{tile.name}: Fix scrap stacking");
                            }
                        }
                        break;

                    // --- MANOR ---

                    // kitchen
                    case "KitchenTile":
                        Transform tablesMisc = tile.transform.Find("TablesMisc");
                        if (tablesMisc != null)
                        {
                            BoxCollider coffeeTableA = tablesMisc.Find("ArrangementA/coffeeTable")?.GetComponent<BoxCollider>();
                            BoxCollider coffeeTableB = tablesMisc.Find("ArrangementB/coffeeTable (1)")?.GetComponent<BoxCollider>();
                            if (coffeeTableA != null && coffeeTableB != null)
                            {
                                coffeeTableA.center = coffeeTableB.center;
                                coffeeTableA.size = coffeeTableB.size;
                                Plugin.Logger.LogDebug($"{tile.name}: Adjusted collider on prop \"{coffeeTableA.name}\"");
                            }
                        }
                        break;

                    // foyer
                    case "ManorStartRoom":
                    case "ManorStartRoomSmall":
                        Transform manorStartRoomMesh = tile.transform.Find("ManorStartRoomMesh");
                        Material chandMat = null;
                        if (manorStartRoomMesh != null)
                        {
                            foreach (string chandelier in new string[]{
                                "Chandelier",
                                "Chandelier2"
                            })
                            {
                                Renderer chand = manorStartRoomMesh.Find(chandelier)?.GetComponent<Renderer>();
                                if (chand != null)
                                {
                                    if (chandMat == null)
                                    {
                                        chandMat = Object.Instantiate(chand.sharedMaterial);
                                        HDMaterial.SetAlphaClipping(chandMat, true);
                                    }
                                    chand.sharedMaterial = chandMat;
                                    Plugin.Logger.LogDebug($"{tile.name}: Fix material for \"{chand.name}\"");
                                }
                            }
                        }
                        break;

                    // spiral stairs
                    case "ManorStairTile":
                        ventSpawn = tile.transform.Find("Vents/Vent (8)");
                        if (ventSpawn != null)
                        {
                            ventSpawn.localPosition = new(ventSpawn.localPosition.x, -5.96f, ventSpawn.localPosition.z);
                            Plugin.Logger.LogDebug($"{tile.name}: Corrected position for \"{ventSpawn.name}\"");
                        }
                        break;

                    // greenhouse
                    case "GreenhouseTile":
                        string[] sinkColliderPaths =
                        [
                            "Colliders/Cube (4)",
                            "Colliders/Cube (6)",
                            "Colliders/Cube (7)",
                            "Colliders/Cube (5)",
                        ];
                        foreach (string sinkColliderPath in sinkColliderPaths)
                        {
                            GameObject sinkCollider = tile.transform.Find(sinkColliderPath)?.gameObject;
                            // remove colliders for old unused sink prop
                            if (sinkCollider != null)
                            {
                                sinkCollider.SetActive(false);
                                Plugin.Logger.LogDebug($"{tile.name}: Removed unused collider \"{sinkCollider.name}\"");
                            }
                        }
                        break;

                    // garage
                    case "GarageTile":
                        // v70PLF doesn't have any null check and causes a crash if my changes are applied
                        if (Chainloader.PluginInfos.ContainsKey(Compatibility.GUID_V70_POWERED_LIGHTS_FIX))
                        {
                            Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - V70PoweredLights Fix detected");
                            break;
                        }
                        Transform hangingLEDBarLight4 = tile.transform.Find("HangingLEDBarLight (4)");
                        Transform indirectLight1 = hangingLEDBarLight4?.Find("IndirectLight (1)");
                        if (indirectLight1 != null && hangingLEDBarLight4.Find("IndirectLight") == null)
                        {
                            indirectLight1.name = "IndirectLight";
                            Plugin.Logger.LogDebug($"{tile.name}: Corrected child reference for \"{hangingLEDBarLight4.name}\"");
                        }
                        break;

                    // --- MINESHAFT ---

                    // tunnels
                    case "TunnelSplit":
                    case "TunnelSplitBranchStart Variant":
                    case "TunnelSplitBranchStart2 Variant":
                        alleyExitDoorContainer = tile.transform.Find("Props/PropSet2/AlleyExitDoorContainer");
                        if (alleyExitDoorContainer != null)
                        {
                            alleyExitDoorContainer.localScale = MINESHAFT_FIRE_EXIT_SCALE;
                            fireExitLight = alleyExitDoorContainer.Find("FireExitDoor/Light");
                            if (fireExitLight != null)
                            {
                                fireExitLight.localPosition = new(0.229f, fireExitLight.localPosition.y, fireExitLight.localPosition.z);
                                Plugin.Logger.LogDebug($"{tile.name}: Corrected scale for \"{alleyExitDoorContainer.name}\"");
                            }
                        }
                        alleyExitDoorContainer = tile.transform.Find("EastWallProps/AlleyExitDoorContainer (1)");
                        if (alleyExitDoorContainer != null)
                        {
                            alleyExitDoorContainer.localScale = MINESHAFT_FIRE_EXIT_SCALE;
                            Plugin.Logger.LogDebug($"{tile.name}: Corrected scale for \"{alleyExitDoorContainer.name}\"");
                        }
                        fixVentsSAndNW = true;
                        break;
                    case "TunnelSplitEndTile":
                        foreach (string fireExit in new string[]{
                            "AlleyExitDoorContainer",
                            "AlleyExitDoorContainer (1)"
                        })
                        {
                            alleyExitDoorContainer = tile.transform.Find(fireExit);
                            if (alleyExitDoorContainer != null)
                            {
                                alleyExitDoorContainer.localScale = MINESHAFT_FIRE_EXIT_SCALE;
                                Plugin.Logger.LogDebug($"{tile.name}: Corrected scale for \"{alleyExitDoorContainer.name}\"");
                            }
                        }
                        fixVentsSAndNW = true;
                        break;

                    // ramp
                    case "GrandTunnelRamp":
                        foreach (string fireExit in new string[]{
                            "AlleyExitDoorContainerN",
                            "AlleyExitDoorContainerS"
                        })
                        {
                            alleyExitDoorContainer = tile.transform.Find(fireExit);
                            if (alleyExitDoorContainer != null)
                            {
                                alleyExitDoorContainer.localScale = MINESHAFT_FIRE_EXIT_SCALE;
                                fireExitLight = alleyExitDoorContainer.Find("FireExitDoor/Light");
                                if (fireExitLight != null)
                                {
                                    fireExitLight.localPosition = new(0.052f, fireExitLight.localPosition.y, fireExitLight.localPosition.z);
                                    Plugin.Logger.LogDebug($"{tile.name}: Corrected scale for \"{alleyExitDoorContainer.name}\"");
                                }
                            }
                        }
                        foreach (string breakerBox in new string[]{
                            "BreakerBoxSpawn",
                            "BreakerBoxSpawn (1)"
                        })
                        {
                            breakerBoxSpawn = tile.transform.Find(breakerBox);
                            if (breakerBoxSpawn != null)
                            {
                                breakerBoxSpawn.localRotation = Quaternion.Euler(breakerBoxSpawn.localEulerAngles.x, breakerBoxSpawn.localEulerAngles.y + 180f, breakerBoxSpawn.localEulerAngles.z);
                                Plugin.Logger.LogDebug($"{tile.name}: Corrected rotation for \"{breakerBoxSpawn.name}\"");
                            }
                        }
                        break;
                }

                bool cave = tile.name.StartsWith("Cave");
                if (cave || tile.name.Contains("Tunnel"))
                {
                    ParticleSystemRenderer[] particleSystemRenderers = tile.GetComponentsInChildren<ParticleSystemRenderer>();
                    foreach (ParticleSystemRenderer partSysRend in particleSystemRenderers)
                    {
                        if (partSysRend.sharedMaterial != null && partSysRend.sharedMaterial.name.StartsWith("RainParticle") && !partSysRend.name.StartsWith("RainHit") && partSysRend.renderMode != ParticleSystemRenderMode.VerticalBillboard)
                        {
                            partSysRend.renderMode = ParticleSystemRenderMode.VerticalBillboard;
                            Plugin.Logger.LogDebug($"{tile.name}: Fixed drip billboarding");
                        }
                    }

                    if (cave)
                    {
                        LODGroup[] lodGroups = tile.GetComponentsInChildren<LODGroup>();
                        foreach (LODGroup lodGroup in lodGroups)
                        {
                            if (lodGroup.fadeMode != LODFadeMode.None)
                            {
                                lodGroup.fadeMode = LODFadeMode.None;
                                Plugin.Logger.LogDebug($"{tile.name}: Fixed LOD fade for \"{lodGroup.name}\"");
                            }
                        }
                    }
                }

                Animator[] animators = tile.GetComponentsInChildren<Animator>();
                if (animators != null && animators.Length > 0)
                {
                    foreach (Animator animator in animators)
                    {
                        if (!animator.CompareTag("PoweredLight") && animator.runtimeAnimatorController != null && (animator.runtimeAnimatorController.name.StartsWith("MineshaftSpotlight") || animator.runtimeAnimatorController.name.StartsWith("LEDHangingLight")))
                        {
                            animator.tag = "PoweredLight";
                            Plugin.Logger.LogDebug($"{tile.name}: Fix power state for \"{animator.name}\"");
                        }
                    }
                }

                if (removeAnomalySpawns)
                {
                    Transform anomalySpawns = tile.transform.Find("AnomalySpawns");
                    if (anomalySpawns != null)
                    {
                        anomalySpawns.gameObject.SetActive(false);
                        Plugin.Logger.LogDebug($"{tile.name}: Removed obsolete triggers (fix spray paint)");
                    }
                    removeAnomalySpawns = false;
                }

                if (fixVentsSAndNW)
                {
                    ventSpawn = tile.transform.Find("VentSpawnS");
                    if (ventSpawn != null)
                    {
                        ventSpawn.localPosition = new(ventSpawn.localPosition.x, ventSpawn.localPosition.y, 0.1f);
                        Plugin.Logger.LogDebug($"{tile.name}: Corrected position for \"{ventSpawn.name}\"");
                    }
                    ventSpawn = tile.transform.Find("NorthWallVentSpawn");
                    if (ventSpawn != null)
                    {
                        ventSpawn.localPosition = new(ventSpawn.localPosition.x, ventSpawn.localPosition.y, 7.901f);
                        Plugin.Logger.LogDebug($"{tile.name}: Corrected position for \"{ventSpawn.name}\"");
                    }
                    fixVentsSAndNW = false;
                }
            }
        }

        static List<GameObject> GetAllTiles(IndoorMapType[] indoorMapTypes)
        {
            List<TileSet> tileSets = [];
            HashSet<GameObject> allTiles = [];

            foreach (IndoorMapType indoorMapType in indoorMapTypes)
            {
                if (System.Array.IndexOf(GlobalReferences.INTERIOR_WHITELIST, indoorMapType.dungeonFlow.name) < 0)
                    continue;

                foreach (GraphNode node in indoorMapType.dungeonFlow.Nodes)
                    tileSets.AddRange(node.TileSets);

                foreach (GraphLine line in indoorMapType.dungeonFlow.Lines)
                {
                    foreach (DungeonArchetype dungeonArchetype in line.DungeonArchetypes)
                    {
                        tileSets.AddRange(dungeonArchetype.TileSets);
                        tileSets.AddRange(dungeonArchetype.BranchStartTileSets);
                        tileSets.AddRange(dungeonArchetype.BranchCapTileSets);
                    }
                }

                foreach (TileSet tileSet in tileSets)
                {
                    foreach (GameObjectChance tileWeight in tileSet.TileWeights.Weights)
                    {
                        if (allTiles.Add(tileWeight.Value))
                            Plugin.Logger.LogDebug($"Cached reference to tile \"{tileWeight.Value.name}\"");
                    }
                }
            }

            return [.. allTiles];
        }
    }
}
