using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class ShipLightPatches
    {
        [HarmonyPatch(typeof(CozyLights), nameof(CozyLights.Update))]
        [HarmonyPrefix]
        static bool CozyLights_Pre_Update(CozyLights __instance)
        {
            if (StartOfRound.Instance != null && StartOfRound.Instance.firingPlayersCutsceneRunning && GlobalReferences.shipAnimator != null && GlobalReferences.shipAnimator.GetBool("AlarmRinging"))
            {
                if (__instance.cozyLightsOn)
                {
                    __instance.cozyLightsAnimator.SetBool("on", false);
                    __instance.cozyLightsOn = false;
                }

                if (__instance.turnOnAudio != null)
                    __instance.SetAudio();

                return false;
            }

            return true;
        }
    }
}
