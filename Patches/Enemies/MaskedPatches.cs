﻿using ButteryFixes.Utility;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    internal class MaskedPatches
    {
        [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.Update))]
        [HarmonyPostfix]
        static void MaskedPlayerEnemyPostUpdate(MaskedPlayerEnemy __instance)
        {
            if (__instance.maskFloodParticle.isEmitting && __instance.inSpecialAnimationWithPlayer != null)
            {
                // bonus effect: cover the player's face with blood
                __instance.inSpecialAnimationWithPlayer.bodyBloodDecals[3].SetActive(true);
                // enables the blood spillage effect that Zeekerss removed in v49
                if (__instance.inSpecialAnimationWithPlayer == GameNetworkManager.Instance.localPlayerController && !HUDManager.Instance.HUDAnimator.GetBool("biohazardDamage"))
                {
                    HUDManager.Instance.HUDAnimator.SetBool("biohazardDamage", true);
                    Plugin.Logger.LogInfo("Enable screen blood for mask vomit animation");
                }
            }
        }

        [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.FinishKillAnimation))]
        [HarmonyPrefix]
        static void MaskedPlayerEnemyPreFinishKillAnimation(MaskedPlayerEnemy __instance)
        {
            // this should properly prevent the blood effect from persisting after you are rescued from a mask
            // reasons this didn't work in v49 (and presumably why it got removed):
            // - inSpecialAnimationWithPlayer was set to null before checking if it matched the local player
            // - just disabling biohazardDamage wasn't enough to transition back to a normal HUD animator state (it needs a trigger set as well)
            if (__instance.inSpecialAnimationWithPlayer == GameNetworkManager.Instance.localPlayerController && HUDManager.Instance.HUDAnimator.GetBool("biohazardDamage"))
            {
                // cancel the particle effect early, just in case (to prevent it from retriggering and becoming stuck)
                if (__instance.maskFloodParticle.isEmitting)
                    __instance.maskFloodParticle.Stop();
                HUDManager.Instance.HUDAnimator.SetBool("biohazardDamage", false);
                HUDManager.Instance.HUDAnimator.SetTrigger("HealFromCritical");
                Plugin.Logger.LogInfo("Vomit animation was interrupted while blood was on screen");
            }
        }

        [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.KillEnemy))]
        [HarmonyPostfix]
        static void MaskedPlayerEnemyPostKillEnemy(MaskedPlayerEnemy __instance)
        {
            Animator mapDot = __instance.transform.Find("Misc/MapDot")?.GetComponent<Animator>();
            if (mapDot != null)
            {
                mapDot.enabled = false;
                Plugin.Logger.LogInfo("Stop animating masked radar dot");
            }
        }

        [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.SetSuit))]
        [HarmonyPostfix]
        static void PostSetSuit(MaskedPlayerEnemy __instance, int suitId)
        {
            Transform spine = __instance.animationContainer.Find("metarig/spine");
            if (spine == null)
                return;

            // on second thought, not sure parenting prefabs to NetworkObjects is stable or even supported
            try
            {
                if (suitId < StartOfRound.Instance.unlockablesList.unlockables.Count)
                {
                    UnlockableItem suit = StartOfRound.Instance.unlockablesList.unlockables[suitId];
                    if (suit.headCostumeObject != null)
                    {
                        Transform spine004 = spine.Find("spine.001/spine.002/spine.003/spine.004");
                        if (spine004 != null && spine004.Find(suit.headCostumeObject.name + "(Clone)") == null)
                        {
                            Object.Instantiate(suit.headCostumeObject, spine004.position, spine004.rotation, spine004);
                            Plugin.Logger.LogInfo($"Mimic #{__instance.GetInstanceID()} equipped {suit.unlockableName} head");
                        }
                    }
                    if (suit.lowerTorsoCostumeObject != null && spine.Find(suit.lowerTorsoCostumeObject.name + "(Clone)") == null)
                    {
                        Object.Instantiate(suit.lowerTorsoCostumeObject, spine.position, spine.rotation, spine);
                        Plugin.Logger.LogInfo($"Mimic #{__instance.GetInstanceID()} equipped {suit.unlockableName} torso");
                    }
                }
            }
            catch (System.Exception e)
            {
                Plugin.Logger.LogError("Encountered a non-fatal error while attaching costume pieces to mimic");
                Plugin.Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.SetEnemyOutside))]
        [HarmonyPostfix]
        static void MaskedPlayerEnemyPostSetEnemyOutside(MaskedPlayerEnemy __instance)
        {
            if (__instance.timeSinceSpawn > 40f)
                return;

            Transform spine003 = __instance.maskTypes[0].transform.parent.parent;

            Renderer betaBadgeMesh = spine003.Find("BetaBadge")?.GetComponent<Renderer>();
            if (betaBadgeMesh != null)
            {
                betaBadgeMesh.enabled = __instance.mimickingPlayer.playerBetaBadgeMesh.enabled;
                Plugin.Logger.LogInfo($"Mimic #{__instance.GetInstanceID()} VIP: {betaBadgeMesh.enabled}");
            }
            MeshFilter badgeMesh = spine003.Find("LevelSticker")?.GetComponent<MeshFilter>();
            if (badgeMesh != null)
            {
                badgeMesh.mesh = __instance.mimickingPlayer.playerBadgeMesh.mesh;
                Plugin.Logger.LogInfo($"Mimic #{__instance.GetInstanceID()} updated level sticker");
            }

            // toggling GameObjects under a NetworkObject maybe also a bad idea?
            try
            {
                foreach (DecalProjector bloodDecal in __instance.transform.GetComponentsInChildren<DecalProjector>())
                {
                    foreach (GameObject bodyBloodDecal in __instance.mimickingPlayer.bodyBloodDecals)
                    {
                        if (bloodDecal.name == bodyBloodDecal.name)
                        {
                            bloodDecal.gameObject.SetActive(bodyBloodDecal.activeSelf);
                            Plugin.Logger.LogInfo($"Mimic #{__instance.GetInstanceID()} blood: \"{bloodDecal.name}\"");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Plugin.Logger.LogError("Encountered a non-fatal error while enabling mimic blood");
                Plugin.Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.SetMaskType))]
        [HarmonyPrefix]
        static bool PostSetMaskType(MaskedPlayerEnemy __instance, int maskType)
        {
            if (maskType == 5 && __instance.maskTypeIndex != 1)
            {
                __instance.maskTypeIndex = 1;
                Plugin.Logger.LogInfo("Mimic spawned that should be Tragedy");

                // replace the comedy mask's models with the tragedy models
                NonPatchFunctions.ConvertMaskToTragedy(__instance.maskTypes[0].transform);

                // and swap the sound files (these wouldn't work if the tragedy's GameObject was just toggled on)
                RandomPeriodicAudioPlayer randomPeriodicAudioPlayer = __instance.maskTypes[0].GetComponent<RandomPeriodicAudioPlayer>();
                if (randomPeriodicAudioPlayer != null)
                {
                    randomPeriodicAudioPlayer.randomClips = GlobalReferences.tragedyMaskRandomClips;
                    Plugin.Logger.LogInfo("Tragedy mimic cries");
                }
            }

            // need to replace the vanilla behavior entirely because it's just too buggy
            return false;
        }
    }
}
