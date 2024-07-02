using ButteryFixes.Utility;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    internal class NutcrackerPatches
    {
        [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.Update))]
        [HarmonyPostfix]
        static void NutcrackerEnemyAIPostUpdate(NutcrackerEnemyAI __instance, bool ___isLeaderScript)
        {
            // if the leader is dead, manually update the clock
            if (___isLeaderScript && __instance.isEnemyDead)
                ReflectionCache.GLOBAL_NUTCRACKER_CLOCK.Invoke(__instance, null);
        }

        [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.Start))]
        [HarmonyPostfix]
        [HarmonyAfter("Dev1A3.LethalFixes")]
        static void NutcrackerEnemyAIPostStart(NutcrackerEnemyAI __instance, ref bool ___isLeaderScript, ref int ___previousPlayerSeenWhenAiming)
        {
            // 1 in vanilla, but LethalFixes makes it 0.5
            if (__instance.updatePositionThreshold < GlobalReferences.nutcrackerSyncDistance)
                GlobalReferences.nutcrackerSyncDistance = __instance.updatePositionThreshold;

            // fixes nutcracker tiptoe being early when against the host
            ___previousPlayerSeenWhenAiming = -1;

            // if numbersSpawned > 1, a leader might not have been assigned yet (if the first nutcracker spawned with another already queued in a vent)
            if (__instance.IsServer && !___isLeaderScript && __instance.enemyType.numberSpawned > 1)
            {
                NutcrackerEnemyAI[] nutcrackers = Object.FindObjectsOfType<NutcrackerEnemyAI>();
                foreach (NutcrackerEnemyAI nutcracker in nutcrackers)
                {
                    if (nutcracker != __instance && (bool)ReflectionCache.IS_LEADER_SCRIPT.GetValue(nutcracker))
                    {
                        Plugin.Logger.LogDebug($"NUTCRACKER CLOCK: Nutcracker #{__instance.GetInstanceID()} spawned, #{nutcracker.GetInstanceID()} is already leader");
                        return;
                    }
                }
                ___isLeaderScript = true;
                Plugin.Logger.LogInfo($"NUTCRACKER CLOCK: \"Leader\" is still unassigned, promoting #{__instance.GetInstanceID()}");
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
                string status = __instance.isEnemyDead ? "dead" : "alive";
                Plugin.Logger.LogDebug($"NUTCRACKER CLOCK: Leader #{__instance.GetInstanceID()} is {status}, ticked at {prevInspection}, next tick at {NutcrackerEnemyAI.timeAtNextInspection + 2f}");
                prevInspection = -1f;
            }
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.SwitchToBehaviourStateOnLocalClient))]
        [HarmonyPrefix]
        static void PreSwitchToBehaviourStateOnLocalClient(EnemyAI __instance, int stateIndex)
        {
            if (__instance is NutcrackerEnemyAI && stateIndex == 1 && __instance.currentBehaviourStateIndex != 1)
                Plugin.Logger.LogDebug($"NUTCRACKER CLOCK: Nutcracker #{__instance.GetInstanceID()} began inspection at {Time.realtimeSinceStartup}, global inspection time is {NutcrackerEnemyAI.timeAtNextInspection}");
        }
        */

        [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.ReloadGunClientRpc))]
        [HarmonyPostfix]
        static void PostReloadGunClientRpc(NutcrackerEnemyAI __instance)
        {
            if (__instance.gun.shotgunShellLeft.enabled)
            {
                __instance.gun.shotgunShellLeft.enabled = false;
                __instance.gun.shotgunShellRight.enabled = false;
                __instance.gun.StartCoroutine(NonPatchFunctions.ShellsAppearAfterDelay(__instance.gun));
                Plugin.Logger.LogInfo("Shotgun was reloaded by nutcracker; animating shells");
            }
        }

        [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.HitEnemy))]
        [HarmonyPostfix]
        static void NutcrackerEnemyAIPostHitEnemy(NutcrackerEnemyAI __instance, PlayerControllerB playerWhoHit, bool ___aimingGun, bool ___reloadingGun, float ___timeSinceSeeingTarget)
        {
            if (playerWhoHit != null)
            {
                int id = (int)playerWhoHit.playerClientId;
                // "sense" the player hitting it, this allows turning while frozen in place
                if (__instance.IsOwner && !__instance.isEnemyDead && __instance.currentBehaviourStateIndex == 2 && !___aimingGun && !___reloadingGun && (id == __instance.lastPlayerSeenMoving || ___timeSinceSeeingTarget > 0.5f))
                    __instance.SwitchTargetServerRpc(id);
            }
        }

        [HarmonyPatch(typeof(NutcrackerEnemyAI), "AimGun", MethodType.Enumerator)]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> NutcrackerEnemyAITransAimGun(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo timesSeeingSamePlayer = AccessTools.Field(typeof(NutcrackerEnemyAI), "timesSeeingSamePlayer");
            FieldInfo inSpecialAnimation = AccessTools.Field(typeof(EnemyAI), nameof(EnemyAI.inSpecialAnimation));
            FieldInfo updatePositionThreshold = AccessTools.Field(typeof(EnemyAI), nameof(EnemyAI.updatePositionThreshold));
            for (int i = 2; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_0 && codes[i + 1].opcode == OpCodes.Stfld && (FieldInfo)codes[i + 1].operand == timesSeeingSamePlayer)
                {
                    codes[i].opcode = OpCodes.Ldc_I4_1;
                    Plugin.Logger.LogDebug("Transpiler (Nutcracker aim): Reset times saw same player to 1, not 0");
                }
                else if (codes[i].opcode == OpCodes.Stfld && (FieldInfo)codes[i].operand == inSpecialAnimation && codes[i - 2].opcode == OpCodes.Ldloc_1)
                {
                    // instead of setting inSpecialAnimation to true
                    if (codes[i - 1].opcode == OpCodes.Ldc_I4_1)
                    {
                        // change updatePositionThreshold to a smaller value
                        codes[i - 1].opcode = OpCodes.Ldc_R4;
                        codes[i - 1].operand = 0.3f;
                        Plugin.Logger.LogDebug("Transpiler (Nutcracker aim): Sync position while tiptoeing");
                    }
                    // instead of setting inSpecialAnimation to false
                    else
                    {
                        // change updatePositionThreshold to the original value
                        codes[i - 1].opcode = OpCodes.Ldsfld;
                        codes[i - 1].operand = AccessTools.Field(typeof(GlobalReferences), nameof(GlobalReferences.nutcrackerSyncDistance));
                        Plugin.Logger.LogDebug("Transpiler (Nutcracker aim): Dynamic update threshold");
                    }
                    codes[i].operand = updatePositionThreshold;
                }
            }

            return codes;
        }
    }
}
