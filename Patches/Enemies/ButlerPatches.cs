using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    internal class ButlerPatches
    {
        [HarmonyPatch(typeof(ButlerEnemyAI), nameof(ButlerEnemyAI.Start))]
        [HarmonyPostfix]
        static void ButlerEnemyAI_Post_Start(ButlerEnemyAI __instance)
        {
            if (Configuration.scanImprovements.Value)
                ButlerRadar.SpawnButler(__instance);
        }
    }
}