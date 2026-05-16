using ButteryFixes.Utility;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(MeteorShowers))]
    static class MeteorShowersPatches
    {
        [HarmonyPatch(nameof(MeteorShowers.ResetMeteorWeather))]
        [HarmonyPrefix]
        static bool MeteorShowers_Pre_ResetMeteorWeather()
        {
            return Time.realtimeSinceStartup - GlobalReferences.meteorRPCReceivedTime > 10f;
        }
    }
}
