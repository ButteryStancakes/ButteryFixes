using ButteryFixes.Utility;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
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
        //[HarmonyAfter("Dev1A3.LethalFixes")]
        static void NutcrackerEnemyAIPostStart(NutcrackerEnemyAI __instance, ref bool ___isLeaderScript, ref int ___previousPlayerSeenWhenAiming)
        {
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

        // probably unnecessary?
        /*[HarmonyPatch(typeof(NutcrackerEnemyAI), "AimGun", MethodType.Enumerator)]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> NutcrackerEnemyAITransAimGun(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo updatePositionThreshold = AccessTools.Field(typeof(EnemyAI), nameof(EnemyAI.updatePositionThreshold));
            for (int i = 2; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Stfld && (FieldInfo)codes[i].operand == updatePositionThreshold && codes[i - 1].opcode == OpCodes.Ldc_R4 && (float)codes[i - 1].operand == 0.45f)
                {
                    // change updatePositionThreshold to a smaller value
                    codes[i - 1].operand = 0.3f;
                    Plugin.Logger.LogDebug("Transpiler (Nutcracker aim): Improve positionaal accuracy during \"tiptoe\"");
                }
            }

            return codes;
        }*/
    }
}
