using GameNetcodeStuff;
using HarmonyLib;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(SandWormAI))]
    internal class EarthLeviathanPatches
    {
        [HarmonyPatch(nameof(SandWormAI.EatPlayer))]
        [HarmonyPrefix]
        static bool SandWormAI_Pre_EatPlayer(PlayerControllerB playerScript)
        {
            return !playerScript.isInHangarShipRoom;
        }

        [HarmonyPatch(nameof(SandWormAI.OnCollideWithEnemy))]
        [HarmonyPrefix]
        static bool SandWormAI_Pre_OnCollideWithEnemy(SandWormAI __instance, EnemyAI enemyScript)
        {
            return __instance.enemyType != enemyScript.enemyType;
        }
    }
}
