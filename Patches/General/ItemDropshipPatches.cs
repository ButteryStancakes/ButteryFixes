using ButteryFixes.Utility;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(ItemDropship))]
    internal class ItemDropshipPatches
    {
        [HarmonyPatch(nameof(ItemDropship.Start))]
        [HarmonyPostfix]
        static void ItemDropship_Post_Start(ItemDropship __instance)
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
        }
    }
}
