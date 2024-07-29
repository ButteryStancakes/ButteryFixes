using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class ShipLightPatches
    {
        [HarmonyPatch(typeof(CozyLights), "Update")]
        [HarmonyPrefix]
        static bool CozyLightsPreUpdate(CozyLights __instance, ref bool ___cozyLightsOn)
        {
            if (StartOfRound.Instance != null && StartOfRound.Instance.firingPlayersCutsceneRunning && GlobalReferences.shipAnimator != null && GlobalReferences.shipAnimator.GetBool("AlarmRinging"))
            {
                if (___cozyLightsOn)
                {
                    __instance.cozyLightsAnimator.SetBool("on", false);
                    ___cozyLightsOn = false;
                }

                if (__instance.turnOnAudio != null)
                    __instance.SetAudio();

                return false;
            }

            return true;
        }
    }
}
