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

        [HarmonyPatch(typeof(SandWormAI), nameof(SandWormAI.OnCollideWithEnemy))]
        [HarmonyPrefix]
        static bool SandWormAI_Pre_OnCollideWithEnemy(SandWormAI __instance, EnemyAI enemyScript)
        {
            return __instance.enemyType != enemyScript.enemyType;
        }
    }
}
