﻿using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches
{
    [HarmonyPatch]
    internal class EnemyPatches
    {
        internal static EnemyType FLOWER_SNAKE;

        static readonly MethodInfo GLOBAL_NUTCRACKER_CLOCK = typeof(NutcrackerEnemyAI).GetMethod("GlobalNutcrackerClock", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo IS_LEADER_SCRIPT = typeof(NutcrackerEnemyAI).GetField("isLeaderScript", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPatch(typeof(ButlerEnemyAI), nameof(ButlerEnemyAI.OnCollideWithPlayer))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ButlerEnemyAITransOnCollideWithPlayer(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo timeSinceStealthStab = typeof(ButlerEnemyAI).GetField("timeSinceStealthStab", BindingFlags.Instance | BindingFlags.NonPublic);
            int startAt = -1, endAt = -1;
            for (int i = 0; i < codes.Count; i++)
            {
                if (startAt >= 0 && endAt >= 0)
                {
                    for (int j = startAt; j <= endAt; j++)
                    {
                        codes[j].opcode = OpCodes.Nop;
                        codes[j].operand = null;
                    }
                    Plugin.Logger.LogInfo("Transpiler: Remove timestamp check (replace with prefix)");
                    return codes;
                }
                else if (startAt == -1)
                {
                    if (codes[i].opcode == OpCodes.Stfld && (FieldInfo)codes[i].operand == timeSinceStealthStab)
                        startAt = i - 2;
                }
                else if (i > startAt && codes[i].opcode == OpCodes.Ret)
                    endAt = i;
            }

            Plugin.Logger.LogError("Butler collision transpiler failed");
            return codes;
        }

        [HarmonyPatch(typeof(ButlerEnemyAI), nameof(ButlerEnemyAI.OnCollideWithPlayer))]
        [HarmonyPrefix]
        static bool ButlerEnemyAIPreOnCollideWithPlayer(ButlerEnemyAI __instance, ref float ___timeSinceStealthStab)
        {
            if (!__instance.isEnemyDead && __instance.currentBehaviourStateIndex != 2)
            {
                if (Time.realtimeSinceStartup - ___timeSinceStealthStab < 10f)
                    return false;

                Plugin.Logger.LogInfo("Butler rolls chance for \"stealth stab\"");
                if (Random.Range(0, 100) < 86)
                {
                    ___timeSinceStealthStab = Time.realtimeSinceStartup;
                    Plugin.Logger.LogInfo("Stealth stab chance failed (won't check for 10s)");
                    return false;
                }
                Plugin.Logger.LogInfo("Stealth stab chance succeeds (aggro for 3s)");
            }

            return true;
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.EnableEnemyMesh))]
        [HarmonyPrefix]
        static void EnemyAIPreEnableEnemyMesh(EnemyAI __instance)
        {
            // minor optimization; this bug only really affects mimics
            if (__instance is not MaskedPlayerEnemy)
                return;

            if (__instance.skinnedMeshRenderers.Length > 0)
            {
                for (int i = 0; i < __instance.skinnedMeshRenderers.Length; i++)
                {
                    if (__instance.skinnedMeshRenderers == null)
                    {
                        __instance.skinnedMeshRenderers = __instance.skinnedMeshRenderers.Where(skinnedMeshRenderer => skinnedMeshRenderer != null).ToArray();
                        Plugin.Logger.LogWarning($"Removed all missing Skinned Mesh Renderers from enemy \"{__instance.name}\"");
                        break;
                    }
                }
            }
            if (__instance.meshRenderers.Length > 0)
            {
                for (int i = 0; i < __instance.meshRenderers.Length; i++)
                {
                    if (__instance.meshRenderers == null)
                    {
                        __instance.meshRenderers = __instance.meshRenderers.Where(meshRenderer => meshRenderer != null).ToArray();
                        Plugin.Logger.LogWarning($"Removed all missing Mesh Renderers from enemy \"{__instance.name}\"");
                        break;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BlobAI), nameof(BlobAI.OnCollideWithPlayer))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> BlobAITransOnCollideWithPlayer(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 2; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Bge_Un && codes[i - 2].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 2].operand == typeof(BlobAI).GetField("angeredTimer", BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    codes[i].opcode = OpCodes.Bgt_Un;
                    Plugin.Logger.LogInfo("Transpiler: Blob taming now possible without angering");
                    return codes;
                }
            }

            return codes;
        }

        [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.Update))]
        [HarmonyPostfix]
        static void NutcrackerEnemyAIPostUpdate(NutcrackerEnemyAI __instance, bool ___isLeaderScript)
        {
            if (___isLeaderScript && __instance.isEnemyDead)
                GLOBAL_NUTCRACKER_CLOCK.Invoke(__instance, null);
        }

        [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.Start))]
        [HarmonyPostfix]
        static void NutcrackerEnemyAIPostStart(NutcrackerEnemyAI __instance, ref bool ___isLeaderScript)
        {
            if (__instance.IsServer && !___isLeaderScript && __instance.enemyType.numberSpawned > 1)
            {
                NutcrackerEnemyAI[] nutcrackers = Object.FindObjectsOfType<NutcrackerEnemyAI>();
                foreach (NutcrackerEnemyAI nutcracker in nutcrackers)
                {
                    if (nutcracker != __instance && (bool)IS_LEADER_SCRIPT.GetValue(nutcracker))
                        return;
                }
                Plugin.Logger.LogInfo($"NUTCRACKER CLOCK: \"Leader\" is still unassigned, promoting #{__instance.GetInstanceID()}");
                ___isLeaderScript = true;
            }
        }

        // DEBUGGING: Verbose Nutcracker global clock logs
        /*
        static float prevInspection = -1f;

        [HarmonyPatch(typeof(NutcrackerEnemyAI), "GlobalNutcrackerClock")]
        [HarmonyPrefix]
        static void PreGlobalNutcrackerClock(bool ___isLeaderScript)
        {
            if (___isLeaderScript && Time.realtimeSinceStartup - NutcrackerEnemyAI.timeAtNextInspection > 2f)
                prevInspection = Time.realtimeSinceStartup;
        }

        [HarmonyPatch(typeof(NutcrackerEnemyAI), "GlobalNutcrackerClock")]
        [HarmonyPostfix]
        static void PostGlobalNutcrackerClock(NutcrackerEnemyAI __instance)
        {
            if (prevInspection >= 0f)
            {
                string live = __instance.isEnemyDead ? "dead" : "alive";
                Plugin.Logger.LogInfo($"NUTCRACKER CLOCK: Leader #{__instance.GetInstanceID()} is {live}, ticked at {prevInspection}, next tick at {NutcrackerEnemyAI.timeAtNextInspection + 2f}");
                prevInspection = -1f;
            }
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.SwitchToBehaviourStateOnLocalClient))]
        [HarmonyPrefix]
        static void PreSwitchToBehaviourStateOnLocalClient(EnemyAI __instance, int stateIndex)
        {
            if (__instance is NutcrackerEnemyAI && stateIndex == 1 && __instance.currentBehaviourStateIndex != 1)
                Plugin.Logger.LogInfo($"NUTCRACKER CLOCK: Nutcracker #{__instance.GetInstanceID()} began inspection at {Time.realtimeSinceStartup}, global inspection time is {NutcrackerEnemyAI.timeAtNextInspection}");
        }
        */

        [HarmonyPatch(typeof(EnemyAI), "SubtractFromPowerLevel")]
        [HarmonyPrefix]
        static void PreSubtractFromPowerLevel(EnemyAI __instance, ref bool ___removedPowerLevel)
        {
            if (__instance is MaskedPlayerEnemy && !___removedPowerLevel && (__instance as MaskedPlayerEnemy).mimickingPlayer != null)
            {
                Plugin.Logger.LogInfo("\"Masked\" was mimicking a player; will not subtract from power level");
                ___removedPowerLevel = true;
            }
        }

        [HarmonyPatch(typeof(FlowerSnakeEnemy), "SetFlappingLocalClient")]
        [HarmonyPostfix]
        public static void PostSetFlappingLocalClient(FlowerSnakeEnemy __instance, bool isMainSnake/*, bool setFlapping*/)
        {
            if (!isMainSnake /*|| setFlapping*/ || __instance.clingingToPlayer != GameNetworkManager.Instance.localPlayerController || !__instance.clingingToPlayer.disablingJetpackControls)
                return;

            for (int i = 0; i < __instance.clingingToPlayer.ItemSlots.Length; i++)
            {
                if (__instance.clingingToPlayer.ItemSlots[i] == null || __instance.clingingToPlayer.ItemSlots[i].isPocketed)
                    continue;

                if (__instance.clingingToPlayer.ItemSlots[i] is JetpackItem)
                {
                    JetpackItem heldJetpack = __instance.clingingToPlayer.ItemSlots[i] as JetpackItem;
                    if ((bool)ItemPatches.JETPACK_ACTIVATED.GetValue(heldJetpack))
                    {
                        __instance.clingingToPlayer.disablingJetpackControls = false;
                        __instance.clingingToPlayer.maxJetpackAngle = -1f;
                        __instance.clingingToPlayer.jetpackRandomIntensity = 0f;
                        Plugin.Logger.LogInfo("Player still using jetpack when tulip snake dropped; re-enable flight controls");
                        return;
                    }
                }
            }
        }
    }
}
