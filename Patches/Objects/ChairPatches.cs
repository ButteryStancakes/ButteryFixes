using ButteryFixes.Utility;
using GameNetcodeStuff;
using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(MoveToExitSpecialAnimation))]
    class MoveToExitSpecialAnimationPatches
    {
        [HarmonyPatch(nameof(MoveToExitSpecialAnimation.OnShipPowerSurge))]
        [HarmonyPostfix]
        static void MoveToExitSpecialAnimation_Post_OnShipPowerSurge(MoveToExitSpecialAnimation __instance)
        {
            // burn the corpse after it spawns
            if (__instance.electricChair && __instance.interactTrigger.lockedPlayer != null)
                GlobalReferences.friedPlayer = __instance.interactTrigger.lockedPlayer.GetComponent<PlayerControllerB>();
        }

        [HarmonyPatch(nameof(MoveToExitSpecialAnimation.Update))]
        [HarmonyPostfix]
        static void MoveToExitSpecialAnimation_Post_Update(MoveToExitSpecialAnimation __instance)
        {
            if (__instance.electricChair)
                return;

            // lock camera when sitting in armchair
            if (GlobalReferences.sittingInArmchair)
            {
                if (!__instance.interactTrigger.isPlayingSpecialAnimation || __instance.interactTrigger.lockedPlayer == null || __instance.interactTrigger.lockedPlayer != GameNetworkManager.Instance.localPlayerController.transform)
                    GlobalReferences.sittingInArmchair = false;
            }
            else if (__instance.listeningForInput || __instance.timeSinceAnimationStarted > 0f && __instance.interactTrigger.lockedPlayer == GameNetworkManager.Instance.localPlayerController.transform)
                GlobalReferences.sittingInArmchair = true;
        }

        [HarmonyPatch(nameof(MoveToExitSpecialAnimation.OnDisable))]
        [HarmonyPostfix]
        static void MoveToExitSpecialAnimation_Post_OnDisable(MoveToExitSpecialAnimation __instance)
        {
            if (!__instance.electricChair)
                GlobalReferences.sittingInArmchair = false;
        }
    }
}
