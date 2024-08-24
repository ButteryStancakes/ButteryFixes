using GameNetcodeStuff;
using HarmonyLib;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    internal class EarthLeviathanPatches
    {
        [HarmonyPatch(typeof(SandWormAI), nameof(SandWormAI.EatPlayer))]
        [HarmonyPrefix]
        static bool PreEatPlayer(PlayerControllerB playerScript)
        {
            return !playerScript.isInHangarShipRoom;
        }
    }
}
