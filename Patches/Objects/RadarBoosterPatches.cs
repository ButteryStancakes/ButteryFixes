using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(RadarBoosterItem))]
    internal class RadarBoosterPatches
    {
        [HarmonyPatch(nameof(RadarBoosterItem.Start))]
        [HarmonyPostfix]
        static void RadarBoosterItem_Post_Start(RadarBoosterItem __instance)
        {
            Transform radarBoosterDot = __instance.transform.Find("RadarBoosterDot");
            if (radarBoosterDot != null && radarBoosterDot.localPosition.y < 1f)
            {
                radarBoosterDot.position += Vector3.up;
                Plugin.Logger.LogDebug($"Radar booster #{__instance.GetInstanceID()}: Nudged radar dot upwards");
            }
        }
    }
}
