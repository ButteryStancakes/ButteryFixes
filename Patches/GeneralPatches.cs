using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace ButteryFixes.Patches
{
    [HarmonyPatch]
    internal class GeneralPatches
    {
        [HarmonyPatch(typeof(QuickMenuManager), "Start")]
        [HarmonyPostfix]
        static void QuickMenuManagerPostStart(QuickMenuManager __instance)
        {
            SpawnableEnemyWithRarity oldBird = __instance.testAllEnemiesLevel.OutsideEnemies.FirstOrDefault(enemy => enemy.enemyType.name == "RadMech");
            if (oldBird != null)
                oldBird.enemyType.requireNestObjectsToSpawn = true;
            Plugin.Logger.LogInfo("Old Birds now require \"nest\" to spawn");
            SpawnableEnemyWithRarity masked = __instance.testAllEnemiesLevel.Enemies.FirstOrDefault(enemy => enemy.enemyType.name == "MaskedPlayerEnemy");
            if (masked != null)
                masked.enemyType.isOutsideEnemy = false;
            Plugin.Logger.LogInfo("\"Masked\" now subtract from indoor power level");
        }

        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        static void StartOfRoundPostAwake(StartOfRound __instance)
        {
            SelectableLevel rend = __instance.levels.FirstOrDefault(level => level.name == "RendLevel");
            if (rend != null)
            {
                for (int i = 0; i < rend.spawnableMapObjects.Length; i++)
                {
                    if (rend.spawnableMapObjects[i].prefabToSpawn != null && rend.spawnableMapObjects[i].prefabToSpawn.name == "SpikeRoofTrapHazard")
                    {
                        rend.spawnableMapObjects[i].requireDistanceBetweenSpawns = true;
                        Plugin.Logger.LogInfo("Rend now properly spaces spike traps");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetStats))]
        [HarmonyPostfix]
        static void StartOfRoundPostResetStats(StartOfRound __instance)
        {
            for (int i = 0; i < __instance.gameStats.allPlayerStats.Length; i++)
                __instance.gameStats.allPlayerStats[i].profitable = 0;
            Plugin.Logger.LogInfo("Cleared \"profitable\" stat for all employees");
        }

        [HarmonyPatch(typeof(StartOfRound), "ResetShipFurniture")]
        [HarmonyPostfix]
        static void PostResetShipFurniture(StartOfRound __instance)
        {
            if (__instance.IsServer)
            {
                Terminal terminal = Object.FindObjectOfType<Terminal>();
                if (terminal != null && terminal.orderedItemsFromTerminal.Count > 0)
                {
                    terminal.orderedItemsFromTerminal.Clear();
                    terminal.SyncGroupCreditsServerRpc(terminal.groupCredits, 0);
                    Plugin.Logger.LogInfo("Dropship inventory was emptied (game over)");
                }
            }
        }
    }
}
