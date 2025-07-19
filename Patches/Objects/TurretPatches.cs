using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(Turret))]
    internal class TurretPatches
    {
        // Default, Room, Colliders
        static LayerMask wallLayers = (1 << 0) | (1 << 8) | (1 << 11);
        static RaycastHit hit2;

        [HarmonyPatch(nameof(Turret.CheckForPlayersInLineOfSight))]
        [HarmonyPostfix]
        static void Turret_Post_CheckForPlayersInLineOfSight(Turret __instance, ref PlayerControllerB __result)
        {
            if (__result != null)
            {
                // cast a line back across the same ray, to see if the first saw through a backface
                if (Physics.Linecast(__instance.hit.point, __instance.centerPoint.position, out hit2, wallLayers, QueryTriggerInteraction.Ignore))
                {
                    //Plugin.Logger.LogDebug($"Canceled Turret #{__instance.NetworkObjectId} aiming at player \"{__result.playerUsername}\", because it appears to be stuck inside a wall (\"{hit2.collider.name}\" on layer {LayerMask.LayerToName(hit2.collider.gameObject.layer)})");
                    __result = null;
                }
            }
        }
    }
}
