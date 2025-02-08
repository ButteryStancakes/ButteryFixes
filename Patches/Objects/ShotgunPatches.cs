using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class ShotgunPatches
    {
        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ReloadGunEffectsClientRpc))]
        [HarmonyPostfix]
        static void PostReloadGunEffectsClientRpc(ShotgunItem __instance, bool start)
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

        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.Update))]
        [HarmonyPostfix]
        static void ShotgunItemPostUpdate(ShotgunItem __instance)
        {
            // shells should render during the reload animation (this specific patch only works for players)
            if (__instance.isReloading)
            {
                __instance.shotgunShellLeft.forceRenderingOff = false;
                __instance.shotgunShellRight.forceRenderingOff = false;
            }
        }

        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.Start))]
        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.DiscardItem))]
        [HarmonyPostfix]
        static void DontRenderShotgunShells(ShotgunItem __instance)
        {
            __instance.shotgunShellLeft.forceRenderingOff = true;
            __instance.shotgunShellRight.forceRenderingOff = true;
        }
    }
}
