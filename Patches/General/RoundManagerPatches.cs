﻿using ButteryFixes.Utility;
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

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
        [HarmonyPrefix]
        static void PreDespawnPropsAtEndOfRound(bool despawnAllItems)
        {
            if (despawnAllItems || !StartOfRound.Instance.isObjectAttachedToMagnet || StartOfRound.Instance.attachedVehicle == null)
                return;

            foreach (GrabbableObject grabObj in Object.FindObjectsOfType<GrabbableObject>())
            {
                if (!grabObj.isInShipRoom && !grabObj.isHeld && grabObj.transform.parent == StartOfRound.Instance.attachedVehicle.transform)
                {
                    Plugin.Logger.LogWarning($"Item \"{grabObj.itemProperties.itemName}\" #{grabObj.GetInstanceID()} is inside the Cruiser, but somehow not marked as collected; will be preserved");
                    GameNetworkManager.Instance.localPlayerController.SetItemInElevator(true, true, grabObj);
                }
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GenerateNewFloor))]
        [HarmonyPrefix]
        static void PreGenerateNewFloor(RoundManager __instance)
        {
            if (__instance.currentLevel.dungeonFlowTypes == null || __instance.currentLevel.dungeonFlowTypes.Length < 1)
            {
                __instance.currentDungeonType = -1;
                SoundManager.Instance.currentLevelAmbience = __instance.currentLevel.levelAmbienceClips;
            }
        }

        [HarmonyPatch(typeof(RoundManager), "Awake")]
        [HarmonyPostfix]
        static void RoundManagerPostAwake(RoundManager __instance)
        {
            TileOverrides.OverrideTiles(__instance.dungeonFlowTypes);
        }
    }
}
