using ButteryFixes.Utility;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Rendering;

namespace ButteryFixes.Patches
{
    [HarmonyPatch]
    internal class PlayerPatches
    {
        static List<PlayerControllerB> bunnyhoppingPlayers = [];
        static bool localCostumeChanged = false;

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        static void PlayerControllerBPostUpdate(PlayerControllerB __instance, bool ___isWalking)
        {
            // ladder patches are disabled for Fast Climbing & BetterLadders compatibility
            if (__instance.isClimbingLadder && !Plugin.DISABLE_LADDER_PATCH)
            {
                __instance.isSprinting = false;
                // fixes residual slope speed
                if (___isWalking)
                    __instance.playerBodyAnimator.SetFloat("animationSpeed", 1f);
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        [HarmonyPostfix]
        static void PostConnectClientToPlayerObject(PlayerControllerB __instance)
        {
            if (Plugin.configGameResolution.Value != GameResolution.DontChange)
            {
                RenderTexture playerScreen = __instance.gameplayCamera.targetTexture;
                if (Plugin.configGameResolution.Value == GameResolution.High)
                {
                    playerScreen.width = 970;
                    playerScreen.height = 580;
                    Plugin.Logger.LogInfo("High resolution applied");
                }
                else
                {
                    playerScreen.width = 620;
                    playerScreen.height = 350;
                    Plugin.Logger.LogInfo("Low resolution applied");
                }
                Plugin.ENABLE_SCAN_PATCH = true;
            }
            else
            {
                Plugin.ENABLE_SCAN_PATCH = false;
                Plugin.Logger.LogInfo("Resolution changes reverted");
            }
            
            // fix some oddities with local player rendering
            if (!Plugin.DISABLE_PLAYERMODEL_PATCHES)
            {
                Renderer scavengerHelmet = __instance.localVisor.Find("ScavengerHelmet")?.GetComponent<Renderer>();
                if (scavengerHelmet != null)
                {
                    scavengerHelmet.shadowCastingMode = ShadowCastingMode.Off;
                    Plugin.Logger.LogInfo("\"Fake helmet\" no longer casts a shadow");
                }
            }
            try
            {
                __instance.playerBadgeMesh.GetComponent<Renderer>().forceRenderingOff = true;
                __instance.playerBetaBadgeMesh.forceRenderingOff = true;
                Plugin.Logger.LogInfo("Hide badges on local player");
            }
            catch (System.Exception e)
            {
                Plugin.Logger.LogWarning("Ran into error fetching local player's badges");
                Plugin.Logger.LogWarning(e);
            }
        }

        [HarmonyPatch(typeof(HUDManager), "UpdateScanNodes")]
        [HarmonyPostfix]
        static void PostUpdateScanNodes(HUDManager __instance, Dictionary<RectTransform, ScanNodeProperties> ___scanNodes)
        {
            if (!Plugin.ENABLE_SCAN_PATCH || GameNetworkManager.Instance.localPlayerController == null)
                return;

            Rect rect = __instance.playerScreenTexture.GetComponent<RectTransform>().rect;
            for (int i = 0; i < __instance.scanElements.Length; i++)
            {
                if (___scanNodes.TryGetValue(__instance.scanElements[i], out ScanNodeProperties scanNodeProperties))
                {
                    Vector3 viewportPos = GameNetworkManager.Instance.localPlayerController.gameplayCamera.WorldToViewportPoint(scanNodeProperties.transform.position);
                    // this places elements in the proper position regardless of resolution (rescaling causes awkward misalignments)
                    __instance.scanElements[i].anchoredPosition = new Vector2(rect.xMin + (rect.width * viewportPos.x), rect.yMin + (rect.height * viewportPos.y));
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DestroyItemInSlot))]
        [HarmonyPostfix]
        static void PostDestroyItemInSlot(PlayerControllerB __instance, int itemSlot)
        {
            // this fix is redundant with LethalFixes, but here just in case the user doesn't have it installed...
            if (!Plugin.LETHAL_FIXES && !__instance.IsOwner && !HUDManager.Instance.itemSlotIcons[itemSlot].enabled && GameNetworkManager.Instance.localPlayerController.ItemSlots[itemSlot] != null)
            {
                HUDManager.Instance.itemSlotIcons[itemSlot].enabled = true;
                Plugin.Logger.LogInfo("Re-enabled inventory icon (likely that another player has just reloaded a shotgun, and it was erroneously disabled)");
            }
        }

        [HarmonyPatch(typeof(SoundManager), "Start")]
        [HarmonyPostfix]
        static void SoundManagerPostStart(SoundManager __instance)
        {
            // fixes the TZP effects persisting when you disconnect and re-enter the game
            __instance.currentMixerSnapshotID = 4;
            __instance.SetDiageticMixerSnapshot(0, 0.2f);
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.UpdateHealthUI))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> TransUpdateHealthUI(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldarg_1 && codes[i + 1].opcode == OpCodes.Ldc_I4_S && (sbyte)codes[i + 1].operand == 20 && codes[i + 2].opcode == OpCodes.Bge)
                {
                    codes[i + 1].operand = 10;
                    Plugin.Logger.LogDebug("Transpiler: Fix critical injury popup threshold");
                    return codes;
                }
            }

            Plugin.Logger.LogDebug("Health UI transpiler failed");
            return codes;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "PlayJumpAudio")]
        [HarmonyPostfix]
        static void PostPlayJumpAudio(PlayerControllerB __instance, bool ___isWalking)
        {
            if (!Plugin.configFixJumpCheese.Value || !__instance.IsServer || StartOfRound.Instance.inShipPhase)
            {
                if (bunnyhoppingPlayers.Count > 0)
                {
                    Plugin.Logger.LogWarning("Bunnyhopping player list has some residual entries");
                    bunnyhoppingPlayers.Clear();
                }
                return;
            }

            if (__instance.isInsideFactory || __instance.isInElevator || __instance.isInHangarShipRoom)
                return;

            EnemyType mouthDog = GlobalReferences.allEnemiesList["MouthDog"];
            if (mouthDog == null || mouthDog.numberSpawned < 1)
                return;

            bool moving = false;
            if (__instance.IsOwner)
                moving = ___isWalking;
            else if (__instance.timeSincePlayerMoving < 0.25f)
            {
                Vector3 deltaDist = __instance.serverPlayerPosition - __instance.oldPlayerPosition;
                deltaDist.y = 0f;
                moving = deltaDist.magnitude > float.Epsilon;
            }

            if (moving)
            {
                Plugin.Logger.LogInfo($"Player \"{__instance.playerUsername}\" is bunnyhopping with dogs outside; creating noise");
                NonPatchFunctions.FakeFootstepAlert(__instance);

                if (!bunnyhoppingPlayers.Contains(__instance))
                    bunnyhoppingPlayers.Add(__instance);
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.LandFromJumpClientRpc))]
        [HarmonyPostfix]
        static void PostLandFromJumpClientRpc(PlayerControllerB __instance)
        {
            if (!bunnyhoppingPlayers.Contains(__instance))
                return;

            Plugin.Logger.LogInfo($"Player \"{__instance.playerUsername}\" landed from bunnyhop");

            if (Plugin.configFixJumpCheese.Value && __instance.IsServer)
            {
                EnemyType mouthDog = GlobalReferences.allEnemiesList["MouthDog"];
                if (mouthDog != null && mouthDog.numberSpawned >= 1)
                    NonPatchFunctions.FakeFootstepAlert(__instance);
            }

            bunnyhoppingPlayers.Remove(__instance);
        }

        // TODO: Check animator state during animsync RPC instead
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.PlayFootstepSound))]
        [HarmonyPostfix]
        static void PostPlayFootstepSound(PlayerControllerB __instance)
        {
            if (__instance.IsServer && !__instance.IsOwner)
                NonPatchFunctions.playerWasLastSprinting[__instance.actualClientId] = __instance.playerBodyAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Sprinting");
        }

        /*[HarmonyPatch(typeof(PlayerControllerB), "UpdatePlayerAnimationClientRpc")]
        [HarmonyPostfix]
        static void PostUpdatePlayerAnimationClientRpc(PlayerControllerB __instance)
        {
        }*/

        [HarmonyPatch(typeof(UnlockableSuit), nameof(UnlockableSuit.ChangePlayerCostumeElement))]
        [HarmonyPrefix]
        static void PreChangePlayerCostumeElement(ref Transform costumeContainer, GameObject newCostume)
        {
            if (Plugin.DISABLE_PLAYERMODEL_PATCHES)
                return;

            // MoreCompany changes player suits before the local player is initialized which would cause this function to throw an exception
            if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
                return;

            if (costumeContainer == GameNetworkManager.Instance.localPlayerController.headCostumeContainerLocal)
            {
                costumeContainer = GameNetworkManager.Instance.localPlayerController.headCostumeContainer;
                if (newCostume != null)
                    localCostumeChanged = true;
            }
            else if (costumeContainer == GameNetworkManager.Instance.localPlayerController.lowerTorsoCostumeContainer && newCostume != null)
                localCostumeChanged = true;
        }

        [HarmonyPatch(typeof(UnlockableSuit), nameof(UnlockableSuit.ChangePlayerCostumeElement))]
        [HarmonyPostfix]
        static void PostChangePlayerCostumeElement(ref Transform costumeContainer)
        {
            if (localCostumeChanged)
            {
                localCostumeChanged = false;
                foreach (Renderer rend in costumeContainer.GetComponentsInChildren<Renderer>())
                    rend.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                Plugin.Logger.LogInfo($"Local costume part only draws shadow - {costumeContainer.name}");
            }
        }

        [HarmonyPatch(typeof(UnlockableSuit), nameof(UnlockableSuit.SwitchSuitForPlayer))]
        [HarmonyPostfix]
        static void PostSwitchSuitForPlayer(PlayerControllerB player, int suitID)
        {
            // to draw bunny tail in shadow
            if (GameNetworkManager.Instance.localPlayerController == player)
                UnlockableSuit.ChangePlayerCostumeElement(player.lowerTorsoCostumeContainer, StartOfRound.Instance.unlockablesList.unlockables[suitID].lowerTorsoCostumeObject);
        }

        [HarmonyPatch(typeof(DeadBodyInfo), "Start")]
        [HarmonyPostfix]
        static void DeadBodyInfoPostStart(DeadBodyInfo __instance)
        {
            if (Plugin.DISABLE_PLAYERMODEL_PATCHES)
                return;

            SkinnedMeshRenderer mesh = __instance.GetComponentInChildren<SkinnedMeshRenderer>();
            if (mesh == null || StartOfRound.Instance != null)
            {
                UnlockableItem suit = StartOfRound.Instance.unlockablesList.unlockables[__instance.playerScript.currentSuitID];
                if (suit == null)
                    return;

                try
                {
                    Material suitMaterial = mesh.sharedMaterials[0];

                    // special handling for explosions
                    bool burnt = __instance.causeOfDeath == CauseOfDeath.Blast;
                    if (!burnt && __instance.causeOfDeath == CauseOfDeath.Gravity)
                    {
                        foreach (JetpackItem jetpack in Object.FindObjectsOfType<JetpackItem>())
                        {
                            // player died crashing (and exploding) a jetpack
                            if ((bool)ReflectionCache.JETPACK_BROKEN.GetValue(jetpack) && (PlayerControllerB)ReflectionCache.JETPACK_ITEM_PREVIOUS_PLAYER_HELD_BY.GetValue(jetpack) == __instance.playerScript)
                            {
                                burnt = true;
                                Plugin.Logger.LogInfo("Player corpse should be burnt since they crashed a jetpack");
                                break;
                            }
                        }
                    }

                    if (burnt)
                    {
                        // for landmines, old bird missiles, etc.
                        if (suitMaterial != GlobalReferences.scavengerSuitBurnt)
                        {
                            suitMaterial = GlobalReferences.scavengerSuitBurnt;
                            mesh.sharedMaterial = suitMaterial;
                        }
                        // blowing up the cruiser probably shouldn't spawn the "melted" player corpse, instead use the normal model
                        else
                            __instance.ChangeMesh(GlobalReferences.playerBody);
                    }

                    bool snipped = false;

                    if (__instance.detachedHeadObject != null && __instance.detachedHeadObject.TryGetComponent(out Renderer headRend))
                    {
                        // fixes helmet
                        if (__instance.detachedHeadObject.name != "DecapitatedLegs")
                            headRend.material = suitMaterial;
                        // or legs, if you are killed by the barber
                        else
                        {
                            snipped = true;
                            Material[] materials = headRend.sharedMaterials;
                            materials[0] = suitMaterial;
                            headRend.materials = materials;
                        }

                        Plugin.Logger.LogInfo("Fixed helmet material on player corpse");
                    }

                    if (suit.headCostumeObject == null && suit.lowerTorsoCostumeObject == null)
                        return;

                    // tail costume piece
                    Transform lowerTorso = __instance.transform.Find("spine.001");
                    if (suit.lowerTorsoCostumeObject != null)
                    {
                        Transform tailbone = snipped ? __instance.detachedHeadObject : lowerTorso;
                        if (tailbone != null)
                        {
                            GameObject tail = Object.Instantiate(suit.lowerTorsoCostumeObject, tailbone.position, tailbone.rotation, tailbone);
                            if (!__instance.setMaterialToPlayerSuit || burnt)
                            {
                                foreach (Renderer tailRend in tail.GetComponentsInChildren<Renderer>())
                                    tailRend.material = suitMaterial;
                            }
                            // special offset for snipping
                            if (snipped)
                                tail.transform.SetPositionAndRotation(new Vector3(-0.0400025733f, -0.0654963329f, -0.0346327312f), Quaternion.Euler(19.4403114f, 0.0116598327f, 0.0529587828f));
                            Plugin.Logger.LogInfo("Torso attachment complete for player corpse");
                        }
                    }

                    Transform chest = lowerTorso?.Find("spine.002/spine.003");

                    // hat costume piece
                    if (suit.headCostumeObject != null)
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
                            if (!__instance.setMaterialToPlayerSuit || burnt)
                            {
                                foreach (Renderer hatRend in hat.GetComponentsInChildren<Renderer>())
                                    hatRend.material = suitMaterial;
                            }
                            Plugin.Logger.LogInfo("Head attachment complete for player corpse");
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
                        Plugin.Logger.LogInfo("Badges added to player corpse");
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
        static void DeadBodyInfoPostChangeMesh(DeadBodyInfo __instance)
        {
            if (Plugin.DISABLE_PLAYERMODEL_PATCHES)
                return;

            foreach (Renderer rend in __instance.GetComponentsInChildren<Renderer>())
            {
                if (rend.gameObject.layer == 0 && (rend.name.StartsWith("BetaBadge") || rend.name.StartsWith("LevelSticker")))
                {
                    rend.forceRenderingOff = true;
                    Plugin.Logger.LogInfo($"Player corpse transformed; hide badge \"{rend.name}\"");
                }
            }
        }
    }
}