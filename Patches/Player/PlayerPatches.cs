using ButteryFixes.Utility;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace ButteryFixes.Patches.Player
{
    [HarmonyPatch]
    internal class PlayerPatches
    {
        static List<PlayerControllerB> bunnyhoppingPlayers = new(50);
        //static GrabbableObject previousItem = null;

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        static void PlayerControllerBPostUpdate(PlayerControllerB __instance, bool ___isWalking)
        {
            // ladder patches are disabled for Fast Climbing & BetterLadders compatibility
            if (__instance.isClimbingLadder && !Compatibility.DISABLE_LADDER_PATCH)
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
            if (Configuration.gameResolution.Value != GameResolution.DontChange)
            {
                RenderTexture playerScreen = __instance.gameplayCamera.targetTexture;
                if (Configuration.gameResolution.Value == GameResolution.High)
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
                GlobalReferences.patchScanNodes = true;
            }
            else
            {
                if (GlobalReferences.patchScanNodes)
                    Plugin.Logger.LogInfo("Resolution changes reverted");
                GlobalReferences.patchScanNodes = false;
            }

            // fix some oddities with local player rendering
            if (!Compatibility.DISABLE_PLAYERMODEL_PATCHES)
            {
                Renderer scavengerHelmet = __instance.localVisor.Find("ScavengerHelmet")?.GetComponent<Renderer>();
                if (scavengerHelmet != null)
                {
                    scavengerHelmet.shadowCastingMode = ShadowCastingMode.Off;
                    Plugin.Logger.LogDebug("\"Fake helmet\" no longer casts a shadow");
                }
            }
            try
            {
                __instance.playerBadgeMesh.GetComponent<Renderer>().forceRenderingOff = true;
                __instance.playerBetaBadgeMesh.forceRenderingOff = true;
                Plugin.Logger.LogDebug("Hide badges on local player");
            }
            catch (System.Exception e)
            {
                Plugin.Logger.LogWarning("Ran into error fetching local player's badges");
                Plugin.Logger.LogWarning(e);
            }

            if (!Compatibility.INSTALLED_GENERAL_IMPROVEMENTS && __instance.playersManager.mapScreenPlayerName.text == "MONITORING: Player")
            {
                __instance.playersManager.mapScreenPlayerName.SetText($"MONITORING: {__instance.playersManager.mapScreen.radarTargets[__instance.playersManager.mapScreen.targetTransformIndex].name}");
                Plugin.Logger.LogDebug("Fix \"MONITORING: Player\"");
            }

            GlobalReferences.crashedJetpackAsLocalPlayer = false;

            // fix laser pointer shining through walls when pocketed
            foreach (Light light in __instance.allHelmetLights)
            {
                if (light.shadows == LightShadows.None)
                {
                    light.shadows = LightShadows.Hard;
                    light.GetComponent<HDAdditionalLightData>().shadowNearPlane = 0.66f;
                }
            }

            if (Configuration.restoreShipIcon.Value)
            {
                Transform shipIcon = __instance.playersManager.mapScreen.shipArrowUI.transform.Find("ShipIcon");
                if (shipIcon != null)
                {
                    shipIcon.localPosition = new Vector3(shipIcon.localPosition.x, shipIcon.localPosition.y, 0f);
                    Plugin.Logger.LogDebug("Fix ship icon on radar");
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "PlayJumpAudio")]
        [HarmonyPostfix]
        static void PostPlayJumpAudio(PlayerControllerB __instance, bool ___isWalking)
        {
            if (!Configuration.fixJumpCheese.Value || !__instance.IsServer || StartOfRound.Instance.inShipPhase)
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
                Plugin.Logger.LogDebug($"Player \"{__instance.playerUsername}\" is bunnyhopping with dogs outside; creating noise");
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

            Plugin.Logger.LogDebug($"Player \"{__instance.playerUsername}\" landed from bunnyhop");

            if (Configuration.fixJumpCheese.Value && __instance.IsServer)
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

        [HarmonyPatch(typeof(PlayerControllerB), "QEItemInteract_performed")]
        [HarmonyPatch(typeof(PlayerControllerB), "ItemSecondaryUse_performed")]
        [HarmonyPatch(typeof(PlayerControllerB), "ItemTertiaryUse_performed")]
        [HarmonyPrefix]
        static void PreItem_performed(PlayerControllerB __instance)
        {
            if (__instance.equippedUsableItemQE && __instance.currentlyHeldObjectServer != null && (__instance.currentlyHeldObjectServer is FlashlightItem || __instance.currentlyHeldObjectServer is JetpackItem || __instance.currentlyHeldObjectServer is BoomboxItem || __instance.currentlyHeldObjectServer.itemProperties.name == "Hairdryer"))
            {
                __instance.equippedUsableItemQE = false;
                Plugin.Logger.LogWarning("Tried to use Q/E controls on an item with no secondary/tertiary use. This shouldn't happen");
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.PlaceGrabbableObject))]
        [HarmonyPostfix]
        static void PostPlaceGrabbableObject(GrabbableObject placeObject)
        {
            if (StartOfRound.Instance.isObjectAttachedToMagnet && StartOfRound.Instance.attachedVehicle != null && placeObject.transform.parent == StartOfRound.Instance.attachedVehicle.transform)
            {
                GameNetworkManager.Instance.localPlayerController.SetItemInElevator(true, true, placeObject);
                Plugin.Logger.LogDebug($"Item \"{placeObject.itemProperties.itemName}\" #{placeObject.GetInstanceID()} was placed inside a magnetized Cruiser and auto-collected");
            }
        }
    }
}