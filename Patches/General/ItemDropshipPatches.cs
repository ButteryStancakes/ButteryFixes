using ButteryFixes.Utility;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch]
    internal class ItemDropshipPatches
    {
        [HarmonyPatch(typeof(ItemDropship), "Start")]
        [HarmonyPostfix]
        static void ItemDropshipPostStart(ItemDropship __instance)
        {
            // fix doppler level for dropship (both music sources)
            Transform music = __instance.transform.Find("Music");
            if (music != null)
            {
                music.GetComponent<AudioSource>().dopplerLevel = 0.6f * GlobalReferences.dopplerLevelMult;
                AudioSource musicFar = music.Find("Music (1)")?.GetComponent<AudioSource>();
                if (musicFar != null)
                    musicFar.dopplerLevel = 0.6f * GlobalReferences.dopplerLevelMult;
                Plugin.Logger.LogDebug("Doppler level: Dropship");
            }
            // honestly just leave the vehicle version as-is, it's funny
        }
    }
}
