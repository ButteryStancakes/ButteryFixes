using GameNetcodeStuff;
using HarmonyLib;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(SandSpiderAI))]
    internal class SpiderPatches
    {
        // prevents nullref if bunker spider is shot by nutcracker. bug originally described (and fixed) in NutcrackerFixes
        [HarmonyPatch(nameof(SandSpiderAI.TriggerChaseWithPlayer))]
        [HarmonyPrefix]
        static bool SandSpiderAI_Pre_TriggerChaseWithPlayer(PlayerControllerB playerScript)
        {
            return playerScript != null;
        }
    }
}
