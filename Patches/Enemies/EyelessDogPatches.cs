using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    internal class EyelessDogPatches
    {
        [HarmonyPatch(typeof(MouthDogAI), nameof(MouthDogAI.OnCollideWithPlayer))]
        [HarmonyPrefix]
        static bool MouthDogAIPreOnCollideWithPlayer(MouthDogAI __instance, Collider other)
        {
            if (__instance.isEnemyDead || (other.TryGetComponent(out PlayerControllerB player) && player.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed && !StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(__instance.transform.position)))
                return false;

            return true;
        }
    }
}
