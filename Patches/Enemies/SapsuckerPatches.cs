using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(GiantKiwiAI))]
    internal class SapsuckerPatches
    {
        [HarmonyPatch(nameof(GiantKiwiAI.Start))]
        [HarmonyPostfix]
        static void GiantKiwiAI_Post_Start(GiantKiwiAI __instance)
        {
            if (!__instance.IsServer && __instance.birdNest != null)
            {
                Plugin.Logger.LogWarning("Giant Sapsucker cached an invalid nest due to \"test mode\", will fetch again next frame to avoid errors");
                __instance.birdNest = null;
            }
        }
    }
}