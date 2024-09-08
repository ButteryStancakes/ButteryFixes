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
    [HarmonyPatch]
    internal class NutcrackerPatches
    {
        [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.Start))]
        [HarmonyPostfix]
        static void NutcrackerEnemyAIPostStart(NutcrackerEnemyAI __instance, ref bool ___isLeaderScript)
        {
            // GlobalNutcrackerClock()'s logic naturally prevents it from ticking multiple times at once, so there's no issue just marking every nutcracker as a "leader"
            // this is more optimized than using reflection to ensure there is only 1 leader (even if it's less "proper")
            ___isLeaderScript = true;
        }

        [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.ReloadGunClientRpc))]
        [HarmonyPostfix]
        static void PostReloadGunClientRpc(NutcrackerEnemyAI __instance)
        {
            if (__instance.gun.shotgunShellLeft.enabled)
            {
                __instance.gun.shotgunShellLeft.enabled = false;
                __instance.gun.shotgunShellRight.enabled = false;
                __instance.gun.StartCoroutine(NonPatchFunctions.ShellsAppearAfterDelay(__instance.gun));
                Plugin.Logger.LogDebug("Shotgun was reloaded by nutcracker; animating shells");
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

        [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.CheckLineOfSightForLocalPlayer))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> TransCheckLineOfSightForLocalPlayer(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo collidersAndRoomMaskAndDefault = AccessTools.Field(typeof(StartOfRound), nameof(StartOfRound.collidersAndRoomMaskAndDefault));
            for (int i = 1; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && ((MethodInfo)codes[i].operand).Name.Equals(nameof(Physics.Linecast)) && codes[i - 1].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 1].operand == collidersAndRoomMaskAndDefault)
                {
                    codes[i].operand = AccessTools.Method(typeof(Physics), nameof(Physics.Linecast), [typeof(Vector3), typeof(Vector3), typeof(int), typeof(QueryTriggerInteraction)]);
                    codes.Insert(i, new CodeInstruction(OpCodes.Ldc_I4_1));
                    Plugin.Logger.LogDebug("Transpiler (Nutcracker sight): Ignore triggers when spotting player");
                    return codes; // i++;
                }
            }

            Plugin.Logger.LogError("Nutcracker sight transpiler failed");
            return codes;
        }
    }
}
