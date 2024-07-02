using HarmonyLib;

namespace ButteryFixes.Patches.General
{
    internal class BreakerBoxPatches
    {
        [HarmonyPatch(typeof(BreakerBox), nameof(BreakerBox.SetSwitchesOff))]
        [HarmonyPostfix]
        static void PostSetSwitchesOff(BreakerBox __instance)
        {
            __instance.breakerBoxHum.Stop();
        }

        /*[HarmonyPatch(typeof(BreakerBox), nameof(BreakerBox.SwitchBreaker))]
        [HarmonyPostfix]
        static void PostSwitchBreaker(BreakerBox __instance)
        {
            if (__instance.breakerBoxHum.isPlaying && RoundManager.Instance.powerOffPermanently)
                __instance.breakerBoxHum.Stop();
        }*/
    }
}
