using DunGen;
using DunGen.Graph;
using System.Collections.Generic;
using UnityEngine;

namespace ButteryFixes.Utility
{
    internal static class TileOverrides
    {
        internal static void OverrideTiles(IndoorMapType[] indoorMapTypes)
        {
            foreach (GameObject tile in GetAllTiles(indoorMapTypes))
            {
                switch (tile.name)
                {
                    // --- FACTORY ---
                    // - fix items/traps spawning inside the piping on the stair tiles

                    // apparatus rooms
                    case "DoubleDoorRoom":
                    case "MediumRoomHallway1B":
                        Transform anomalySpawns = tile.transform.Find("AnomalySpawns");
                        if (anomalySpawns != null)
                        {
                            anomalySpawns.gameObject.SetActive(false);
                            Plugin.Logger.LogDebug($"{tile.name}: Removed obsolete triggers (fix spray paint)");
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

                    // --- MINESHAFT ---
                }

                if (tile.name.StartsWith("Cave") || tile.name.Contains("Tunnel"))
                {
                    foreach (ParticleSystemRenderer partSysRend in tile.GetComponentsInChildren<ParticleSystemRenderer>())
                    {
                        if (partSysRend.sharedMaterial != null && partSysRend.sharedMaterial.name.StartsWith("RainParticle") && !partSysRend.name.StartsWith("RainHit"))
                        {
                            partSysRend.renderMode = ParticleSystemRenderMode.VerticalBillboard;
                            Plugin.Logger.LogDebug($"{tile.name}: Fix drip billboarding");
                        }
                    }
                }
            }
        }

        static List<GameObject> GetAllTiles(IndoorMapType[] indoorMapTypes)
        {
            List<TileSet> tileSets = [];
            HashSet<GameObject> allTiles = [];

            foreach (IndoorMapType indoorMapType in indoorMapTypes)
            {
                foreach (GraphNode node in indoorMapType.dungeonFlow.Nodes)
                    tileSets.AddRange(node.TileSets);

                foreach (GraphLine line in indoorMapType.dungeonFlow.Lines)
                {
                    foreach (DungeonArchetype dungeonArchetype in line.DungeonArchetypes)
                        tileSets.AddRange(dungeonArchetype.TileSets);
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
