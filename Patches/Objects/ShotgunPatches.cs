﻿using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(ShotgunItem))]
    internal class ShotgunPatches
    {
        [HarmonyPatch(nameof(ShotgunItem.ReloadGunEffectsClientRpc))]
        [HarmonyPostfix]
        static void ShotgunItem_Post_ReloadGunEffectsClientRpc(ShotgunItem __instance, bool start)
        {
            // controls shells appearing/disappearing during reload for all clients (except for the one holding the gun)
            if (start && !__instance.IsOwner)
            {
                __instance.shotgunShellLeft.enabled = __instance.shellsLoaded > 0;
                __instance.shotgunShellRight.enabled = false;
                __instance.StartCoroutine(NonPatchFunctions.ShellsAppearAfterDelay(__instance));
                Plugin.Logger.LogDebug("Shotgun was reloaded by another client; animating shells");
            }
        }

        [HarmonyPatch(nameof(ShotgunItem.Update))]
        [HarmonyPostfix]
        static void ShotgunItem_Post_Update(ShotgunItem __instance)
        {
            // shells should render during the reload animation (this specific patch only works for players)
            if (__instance.isReloading)
            {
                __instance.shotgunShellLeft.forceRenderingOff = false;
                __instance.shotgunShellRight.forceRenderingOff = false;
            }
        }

        [HarmonyPatch(nameof(ShotgunItem.Start))]
        [HarmonyPatch(nameof(ShotgunItem.DiscardItem))]
        [HarmonyPostfix]
        static void DontRenderShotgunShells(ShotgunItem __instance)
        {
            __instance.shotgunShellLeft.forceRenderingOff = true;
            __instance.shotgunShellRight.forceRenderingOff = true;
        }
    }
}
