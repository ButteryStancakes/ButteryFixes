﻿using ButteryFixes.Utility;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class ItemPatches
    {
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.ChargeBatteries))]
        [HarmonyPostfix]
        static void GrabbableObject_Post_ChargeBatteries(GrabbableObject __instance)
        {
            BoomboxItem boomboxItem = __instance as BoomboxItem;
            if (boomboxItem != null)
            {
                // needs to verify charge is > 0 because there's a special pitch effect on battery death we don't want to interrupt
                if (boomboxItem.isPlayingMusic && boomboxItem.boomboxAudio.pitch < 1f && boomboxItem.insertedBattery.charge > 0f)
                {
                    boomboxItem.boomboxAudio.pitch = 1f;
                    Plugin.Logger.LogDebug("Boombox was recharged, correcting pitch");
                }
            }
        }

        // for soccer ball (and also whoopee cushion)
        [HarmonyPatch(typeof(GrabbableObjectPhysicsTrigger), nameof(GrabbableObjectPhysicsTrigger.OnTriggerEnter))]
        [HarmonyPrefix]
        static bool GrabbableObjectPhysicsTrigger_Pre_OnTriggerEnter(GrabbableObjectPhysicsTrigger __instance, Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                if (!other.TryGetComponent(out EnemyAICollisionDetect enemyAICollisionDetect) || enemyAICollisionDetect.mainScript == null)
                    return false;

                if (enemyAICollisionDetect.mainScript.isEnemyDead)
                    return false;

                if (__instance.itemScript.isInShipRoom && (StartOfRound.Instance.shipIsLeaving || !enemyAICollisionDetect.mainScript.isInsidePlayerShip || enemyAICollisionDetect.mainScript is ForestGiantAI || enemyAICollisionDetect.mainScript is RadMechAI))
                    return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.PocketItem))]
        [HarmonyPostfix]
        static void GrabbableObject_Post_PocketItem(GrabbableObject __instance)
        {
            if (__instance.playerHeldBy != null)
            {
                /*if (__instance.playerHeldBy.IsOwner && !string.IsNullOrEmpty(__instance.itemProperties.grabAnim))
                {
                    try
                    {
                        __instance.playerHeldBy.playerBodyAnimator.SetBool(__instance.itemProperties.grabAnim, false);
                    }
                    catch
                    {
                        Plugin.Logger.LogWarning($"Pocketing item {__instance.itemProperties.itemName}, bool \"{__instance.itemProperties.grabAnim}\" does not exist on player animator");
                    }
                }*/
                __instance.playerHeldBy.playerBodyAnimator.SetTrigger(__instance.itemProperties.twoHandedAnimation ? "SwitchHoldAnimationTwoHanded" : "SwitchHoldAnimation");
            }
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.EquipItem))]
        [HarmonyPostfix]
        static void GrabbableObject_Post_EquipItem(GrabbableObject __instance)
        {
            // fix items being too big/small when grabbed out of cruiser or elevator
            if (__instance.transform.lossyScale != __instance.originalScale)
            {
                __instance.transform.SetParent(StartOfRound.Instance.propsContainer);
                __instance.transform.localScale = __instance.originalScale;
            }
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.PlayDropSFX))]
        [HarmonyPatch(typeof(SoccerBallProp), nameof(SoccerBallProp.PlayDropSFX))]
        [HarmonyPostfix]
        static void GrabbableObject_Post_PlayDropSFX(GrabbableObject __instance)
        {
            // emit noise alerts on all clients
            if (__instance.itemProperties.dropSFX != null && !__instance.IsOwner)
                RoundManager.Instance.PlayAudibleNoise(__instance.transform.position, 8f, 0.5f, 0, __instance.isInElevator && StartOfRound.Instance.hangarDoorsClosed, 941);
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LateUpdate))]
        [HarmonyPostfix]
        static void GrabbableObject_Post_LateUpdate(GrabbableObject __instance)
        {
            // fix items spinning forever if you take them out of the microwave
            if (__instance.rotateObject && (__instance.playerHeldBy != null || __instance.parentObject != null))
            {
                __instance.rotateObject = false;
                if (GlobalReferences.microwavedItems.Contains(__instance))
                    GlobalReferences.microwavedItems.Remove(__instance);
            }
        }
    }
}
