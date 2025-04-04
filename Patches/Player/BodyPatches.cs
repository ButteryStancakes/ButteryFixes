﻿using ButteryFixes.Utility;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Player
{
    [HarmonyPatch]
    internal class BodyPatches
    {
        static bool dontCheckRenderers;

        [HarmonyPatch(typeof(DeadBodyInfo), "Start")]
        [HarmonyPostfix]
        static void DeadBodyInfoPostStart(DeadBodyInfo __instance)
        {
            if (__instance.grabBodyObject != null && (__instance.playerScript.isInHangarShipRoom || StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(__instance.grabBodyObject.transform.position) || StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(__instance.playerScript.transform.position)))
                __instance.playerScript.SetItemInElevator(true, true, __instance.grabBodyObject);

            if (Compatibility.DISABLE_PLAYERMODEL_PATCHES)
                return;

            if (__instance.causeOfDeath == CauseOfDeath.Stabbing)
                __instance.MakeCorpseBloody();

            SkinnedMeshRenderer mesh = __instance.GetComponentInChildren<SkinnedMeshRenderer>();
            if (mesh != null && StartOfRound.Instance != null)
            {
                UnlockableItem suit = StartOfRound.Instance.unlockablesList.unlockables[__instance.playerScript.currentSuitID];
                if (suit == null)
                    return;

                try
                {
                    Material suitMaterial = mesh.sharedMaterials[0];

                    // special handling for explosions
                    bool burnt = __instance.causeOfDeath == CauseOfDeath.Blast;
                    if (!burnt && GlobalReferences.crashedJetpackAsLocalPlayer && __instance.playerScript == GameNetworkManager.Instance.localPlayerController)
                    {
                        burnt = true;
                        GlobalReferences.crashedJetpackAsLocalPlayer = false;
                        __instance.setMaterialToPlayerSuit = false;
                        Plugin.Logger.LogInfo("Local player spawned a body after a jetpack crashed, caught in time to burn it");
                    }

                    if (burnt)
                    {
                        // for landmines, old bird missiles, etc.
                        if (suitMaterial != GlobalReferences.scavengerSuitBurnt)
                        {
                            suitMaterial = GlobalReferences.scavengerSuitBurnt;
                            mesh.sharedMaterial = suitMaterial;

                            NonPatchFunctions.SmokingHotCorpse(__instance.transform);
                        }
                        // blowing up the cruiser probably shouldn't spawn the "melted" player corpse, instead use the normal model
                        else
                        {
                            dontCheckRenderers = true;
                            __instance.ChangeMesh(GlobalReferences.playerBody);
                            //dontCheckRenderers = false;
                        }
                    }

                    bool snipped = false;

                    if (__instance.detachedHeadObject != null && __instance.detachedHeadObject.TryGetComponent(out Renderer headRend))
                    {
                        // fixes helmet
                        if (__instance.detachedHeadObject.name != "DecapitatedLegs")
                            headRend.sharedMaterial = suitMaterial;
                        // or legs, if you are killed by the barber
                        else
                        {
                            snipped = true;
                            Material[] materials = headRend.sharedMaterials;
                            materials[0] = suitMaterial;
                            headRend.materials = materials;
                        }

                        Plugin.Logger.LogDebug("Fixed helmet material on player corpse");
                    }

                    Transform lowerTorso = __instance.transform.Find("spine.001");
                    Transform chest = lowerTorso?.Find("spine.002/spine.003");

                    if (suit.headCostumeObject != null || suit.lowerTorsoCostumeObject != null)
                    {
                        // tail costume piece
                        if (suit.lowerTorsoCostumeObject != null)
                        {
                            Transform tailbone = snipped ? __instance.detachedHeadObject : lowerTorso;
                            if (tailbone != null)
                            {
                                GameObject tail = Object.Instantiate(suit.lowerTorsoCostumeObject, tailbone.position, tailbone.rotation, tailbone);
                                if (!__instance.setMaterialToPlayerSuit || burnt)
                                {
                                    foreach (Renderer tailRend in tail.GetComponentsInChildren<Renderer>())
                                        tailRend.sharedMaterial = suitMaterial;
                                }
                                // special offset for snipping
                                if (snipped)
                                    tail.transform.SetPositionAndRotation(new Vector3(-0.0400025733f, -0.0654963329f, -0.0346327312f), Quaternion.Euler(19.4403114f, 0.0116598327f, 0.0529587828f));
                                Plugin.Logger.LogDebug("Torso attachment complete for player corpse");
                            }
                        }

                        // hat costume piece
                        bool party = suit.headCostumeObject.name.StartsWith("PartyHatContainer");
                        if (suit.headCostumeObject != null && (!party || (!burnt && mesh.sharedMaterial != GlobalReferences.scavengerSuitBurnt)))
                        {
                            Transform head = __instance.detachedHeadObject;
                            if ((head == null || snipped) && chest != null)
                                head = chest.Find("spine.004");
                            if (head != null)
                            {
                                GameObject hat = Object.Instantiate(suit.headCostumeObject, head.position, head.rotation, head);
                                // special offset/scale for decapitations
                                if (head == __instance.detachedHeadObject)
                                {
                                    hat.transform.SetPositionAndRotation(new Vector3(0.0698937327f, 0.0544735007f, -0.685245395f), Quaternion.Euler(96.69699f, 0f, 0f));
                                    hat.transform.localScale = new Vector3(hat.transform.localScale.x / head.localScale.x, hat.transform.localScale.y / head.localScale.y, hat.transform.localScale.z / head.localScale.z);
                                }
                                if ((!__instance.setMaterialToPlayerSuit || burnt) && !party)
                                {
                                    foreach (Renderer hatRend in hat.GetComponentsInChildren<Renderer>())
                                        hatRend.sharedMaterial = suitMaterial;
                                }
                                Plugin.Logger.LogDebug("Head attachment complete for player corpse");
                            }
                        }
                    }

                    // badges
                    if (chest != null && __instance.setMaterialToPlayerSuit && !burnt)
                    {
                        Transform badge = Object.Instantiate(__instance.playerScript.playerBadgeMesh.transform, chest);
                        Transform betaBadge = Object.Instantiate(__instance.playerScript.playerBetaBadgeMesh.transform, chest);
                        if (__instance.playerScript == GameNetworkManager.Instance.localPlayerController)
                        {
                            badge.GetComponent<Renderer>().forceRenderingOff = false;
                            betaBadge.GetComponent<Renderer>().forceRenderingOff = false;
                        }
                        Plugin.Logger.LogDebug("Badges added to player corpse");
                    }
                }
                catch (System.Exception e)
                {
                    Plugin.Logger.LogError("Encountered a non-fatal error while adjusting player corpse appearance");
                    Plugin.Logger.LogError(e);
                }
            }
        }

        [HarmonyPatch(typeof(DeadBodyInfo), nameof(DeadBodyInfo.ChangeMesh))]
        [HarmonyPostfix]
        static void DeadBodyInfoPostChangeMesh(DeadBodyInfo __instance, Material changeMaterial)
        {
            if (Compatibility.DISABLE_PLAYERMODEL_PATCHES)
                return;

            if (dontCheckRenderers)
            {
                dontCheckRenderers = false;
                return;
            }

            foreach (Renderer rend in __instance.GetComponentsInChildren<Renderer>())
            {
                if (rend.gameObject.layer == 0 && ((rend.name.StartsWith("BetaBadge") || rend.name.StartsWith("LevelSticker")) || (changeMaterial != null && changeMaterial.name.StartsWith("SpooledPlayerMat"))))
                {
                    rend.forceRenderingOff = true;
                    Plugin.Logger.LogDebug($"Player corpse transformed; hide renderer \"{rend.name}\"");
                }
            }
        }

        [HarmonyPatch(typeof(RagdollGrabbableObject), nameof(RagdollGrabbableObject.Start))]
        [HarmonyPostfix]
        static void RagdollGrabbableObjectPostStart(RagdollGrabbableObject __instance)
        {
            if (StartOfRound.Instance != null && !StartOfRound.Instance.isChallengeFile && StartOfRound.Instance.currentLevel.name != "CompanyBuildingLevel")
                __instance.scrapValue = 0;
        }

        [HarmonyPatch(typeof(DeadBodyInfo), nameof(DeadBodyInfo.SetRagdollPositionSafely))]
        [HarmonyPostfix]
        static void PostSetRagdollPositionSafely(DeadBodyInfo __instance, Vector3 newPosition)
        {
            if (!Compatibility.INSTALLED_GENERAL_IMPROVEMENTS && __instance.grabBodyObject != null && StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(newPosition))
            {
                GameNetworkManager.Instance.localPlayerController.SetItemInElevator(true, true, __instance.grabBodyObject);
                /*if (!__instance.grabBodyObject.isInElevator && !__instance.grabBodyObject.isInShipRoom)
                    StartOfRound.Instance.currentShipItemCount++;
                __instance.grabBodyObject.isInElevator = true;
                __instance.grabBodyObject.isInShipRoom = true;
                RoundManager.Instance.CollectNewScrapForThisRound(__instance.grabBodyObject);*/
            }
        }
    }
}
