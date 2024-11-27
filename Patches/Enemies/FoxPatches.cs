using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    internal class FoxPatches
    {
        [HarmonyPatch(typeof(BushWolfEnemy), nameof(BushWolfEnemy.OnCollideWithPlayer))]
        [HarmonyPrefix]
        static bool BushWolfEnemyPreOnCollideWithPlayer(BushWolfEnemy __instance, Collider other, float ___timeSinceTakingDamage)
        {
            // don't kill players in the ship except in self defense
            if (__instance.isEnemyDead || (other.TryGetComponent(out PlayerControllerB player) && player.isInHangarShipRoom && ___timeSinceTakingDamage > 2.5f))
                return false;

            return true;
        }
    }
}
