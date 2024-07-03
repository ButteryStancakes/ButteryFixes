using GameNetcodeStuff;
using HarmonyLib;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    internal class SpiderPatches
    {
        // prevents nullref if bunker spider is shot by nutcracker. bug originally described (and fixed) in NutcrackerFixes
        [HarmonyPatch(typeof(SandSpiderAI), nameof(SandSpiderAI.TriggerChaseWithPlayer))]
        [HarmonyPrefix]
        static bool SandSpiderAIPreTriggerChaseWithPlayer(PlayerControllerB playerScript)
        {
            return playerScript != null;
        }
    }
}
