using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(BreakerBox))]
    static class BreakerBoxPatches
    {
        [HarmonyPatch(nameof(BreakerBox.SetSwitchesOff))]
        [HarmonyPostfix]
        static void BreakerBox_Post_SetSwitchesOff(BreakerBox __instance)
        {
            __instance.breakerBoxHum.Stop();
        }

        [HarmonyPatch(nameof(BreakerBox.SwitchBreaker))]
        [HarmonyPostfix]
        static void BreakerBox_Post_SwitchBreaker(BreakerBox __instance)
        {
            if (__instance.breakerBoxHum.isPlaying && RoundManager.Instance.powerOffPermanently)
                __instance.breakerBoxHum.Stop();
        }

        [HarmonyPatch(nameof(BreakerBox.Start))]
        [HarmonyPostfix]
        static void BreakerBox_Post_Start(BreakerBox __instance)
        {
            GlobalReferences.breakerBox = __instance;
        }
    }
}
