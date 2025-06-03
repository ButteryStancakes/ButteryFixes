using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(QuickMenuManager))]
    internal class QuickMenuManagerPatches
    {
        [HarmonyPatch(nameof(QuickMenuManager.Start))]
        [HarmonyPostfix]
        static void QuickMenuManagerPostStart(QuickMenuManager __instance)
        {
            // cache all references to enemy types
            GlobalReferences.allEnemiesList.Clear();
            List<SpawnableEnemyWithRarity>[] allEnemyLists =
            [
                __instance.testAllEnemiesLevel.Enemies,
                __instance.testAllEnemiesLevel.OutsideEnemies,
                __instance.testAllEnemiesLevel.DaytimeEnemies
            ];

            foreach (List<SpawnableEnemyWithRarity> enemies in allEnemyLists)
                foreach (SpawnableEnemyWithRarity spawnableEnemyWithRarity in enemies)
                {
                    if (GlobalReferences.allEnemiesList.ContainsKey(spawnableEnemyWithRarity.enemyType.name))
                    {
                        if (GlobalReferences.allEnemiesList[spawnableEnemyWithRarity.enemyType.name] == spawnableEnemyWithRarity.enemyType)
                            Plugin.Logger.LogWarning($"allEnemiesList: Tried to cache reference to \"{spawnableEnemyWithRarity.enemyType.name}\" more than once");
                        else
                            Plugin.Logger.LogWarning($"allEnemiesList: Tried to cache two different enemies by same name ({spawnableEnemyWithRarity.enemyType.name})");
                    }
                    else
                        GlobalReferences.allEnemiesList.Add(spawnableEnemyWithRarity.enemyType.name, spawnableEnemyWithRarity.enemyType);
                }

            ScriptableObjectOverrides.OverrideEnemyTypes();
        }
    }
}
