using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(BushWolfEnemy))]
    internal class FoxPatches
    {
        [HarmonyPatch(nameof(BushWolfEnemy.OnCollideWithPlayer))]
        [HarmonyPrefix]
        static bool BushWolfEnemy_Pre_OnCollideWithPlayer(BushWolfEnemy __instance, Collider other)
        {
            // don't kill players in the ship except in self defense
            if (__instance.isEnemyDead || (other.TryGetComponent(out PlayerControllerB player) && player.isInHangarShipRoom && __instance.timeSinceTakingDamage > 2.5f))
                return false;

            return true;
        }
    }
}
