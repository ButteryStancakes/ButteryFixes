using ButteryFixes.Utility;
using GameNetcodeStuff;
using HarmonyLib;

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
