using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class ElevatorPatches
    {
        [HarmonyPatch(typeof(MineshaftElevatorController), "OnEnable")]
        [HarmonyPostfix]
        static void MineshaftElevatorControllerPostOnEnable(MineshaftElevatorController __instance)
        {
            __instance.elevatorJingleMusic.dopplerLevel = 0.58f * GlobalReferences.dopplerLevelMult;
            Plugin.Logger.LogInfo("Doppler level: Mineshaft elevator");
        }
    }
}
