using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
            Object.FindObjectOfType<BreakerBox>()?.breakerBoxHum.Stop();
        }

        [HarmonyPatch(typeof(RoundManager), "SetExitIDs")]
        [HarmonyPostfix]
        static void PostSetExitIDs(RoundManager __instance)
        {
            if (Configuration.fixFireExits.Value)
            {
                foreach (EntranceTeleport entranceTeleport in Object.FindObjectsOfType<EntranceTeleport>())
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
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldflda, ReflectionCache.SPAWN_PROBABILITIES),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, ReflectionCache.CURRENT_LEVEL),
                        new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(SelectableLevel), enemies)),
                        new CodeInstruction(OpCodes.Call, ReflectionCache.SPAWN_PROBABILITIES_POST_PROCESS)
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
            if (StartOfRound.Instance.currentLevel.name == "DineLevel" || StartOfRound.Instance.currentLevel.name == "ArtificeLevel")
            {
                foreach (GameObject poweredLight in GameObject.FindGameObjectsWithTag("PoweredLight"))
                    if (poweredLight.name.StartsWith("NeonLights") && poweredLight.transform.position.y > -100f)
                    {
                        poweredLight.tag = "Untagged";
                        Plugin.Logger.LogDebug($"{StartOfRound.Instance.currentLevel.PlanetName}: Exterior lights disconnected from interior power");
                    }
            }
        }

        [HarmonyPatch(typeof(RoundManager), "SetToCurrentLevelWeather")]
        [HarmonyPostfix]
        static void PostSetToCurrentLevelWeather(RoundManager __instance)
        {
            if (TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Rainy && Configuration.restoreQuicksand.Value)
            {
                System.Random rand = new System.Random(StartOfRound.Instance.randomMapSeed + 2);
                for (int i = 0; i < rand.Next(5, rand.Next(0, 100) < 7 ? 30 : 15); i++)
                    Object.Instantiate(__instance.quicksandPrefab, __instance.GetRandomNavMeshPositionInBoxPredictable(__instance.outsideAINodes[rand.Next(0, __instance.outsideAINodes.Length)].transform.position, 30f, default, rand, -1) + Vector3.up, Quaternion.identity, __instance.mapPropsContainer.transform);
                Plugin.Logger.LogInfo("Generated quicksand. Note that this *might* cause problems if other players in your lobby aren't using this setting!!");
            }
        }
    }
}
