using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class CruiserPatches
    {
        [HarmonyPatch(typeof(VehicleController), "DestroyCar")]
        [HarmonyPostfix]
        static void PostDestroyCar(VehicleController __instance)
        {
            __instance.hoodAudio.mute = true;
            __instance.healthMeter.GetComponentInChildren<Renderer>().forceRenderingOff = true;
            __instance.turboMeter.GetComponentInChildren<Renderer>().forceRenderingOff = true;
        }
    }
}
