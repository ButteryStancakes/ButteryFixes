using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(MineshaftElevatorController))]
    internal class ElevatorPatches
    {
        [HarmonyPatch(nameof(MineshaftElevatorController.OnEnable))]
        [HarmonyPostfix]
        static void MineshaftElevatorController_Post_OnEnable(MineshaftElevatorController __instance)
        {
            __instance.elevatorJingleMusic.dopplerLevel = 0.58f * GlobalReferences.dopplerLevelMult;
            Plugin.Logger.LogDebug("Doppler level: Mineshaft elevator");
        }
    }
}
