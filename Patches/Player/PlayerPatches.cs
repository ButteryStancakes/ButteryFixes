using ButteryFixes.Utility;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace ButteryFixes.Patches.Player
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerPatches
    {
        static readonly string[] qeBugItems = [
            "Flashlight",
            "ProFlashlight",
            "LaserPointer",
            "Jetpack",
            //"Boombox",
            "Hairdryer"
        ];

        static List<PlayerControllerB> bunnyhoppingPlayers = new(50);

        static float safeTimer = 0, safeTimer2 = 0;

        [HarmonyPatch(nameof(PlayerControllerB.Update))]
        [HarmonyPostfix]
        static void PlayerControllerB_Post_Update(PlayerControllerB __instance)
        {
            // ladder patches are disabled for Fast Climbing & BetterLadders compatibility
            if (__instance.isClimbingLadder && !Compatibility.DISABLE_LADDER_PATCH)
            {
                __instance.isSprinting = false;
                // fixes residual slope speed
                if (__instance.isWalking)
                    __instance.playerBodyAnimator.SetFloat("animationSpeed", 1f);
            }

            if (!Compatibility.DISABLE_INTERACT_FIX)
            {
                if (__instance.isGrabbingObjectAnimation)
                {
                    safeTimer += Time.deltaTime;
                    if (safeTimer > __instance.grabObjectAnimationTime + 0.3f)
                    {
                        Plugin.Logger.LogWarning("Player's interactions probably got stuck - resetting");
                        __instance.isGrabbingObjectAnimation = false;
                    }
                }
                else if (safeTimer > 0f)
                    safeTimer = 0f;

                if (__instance.throwingObject)
                {
                    safeTimer2 += Time.deltaTime;
                    if (safeTimer2 > 2f)
                    {
                        Plugin.Logger.LogWarning("Player's interactions probably got stuck - resetting");
                        __instance.throwingObject = false;
                    }
                }
                else if (safeTimer2 > 0f)
                    safeTimer2 = 0f;
            }
        }

        [HarmonyPatch(nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        [HarmonyPostfix]
        static void PlayerControllerB_Post_ConnectClientToPlayerObject(PlayerControllerB __instance)
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
            Renderer scavengerHelmet = __instance.localVisor.Find("ScavengerHelmet")?.GetComponent<Renderer>();
            if (scavengerHelmet != null)
            {
                scavengerHelmet.shadowCastingMode = ShadowCastingMode.Off;
                Plugin.Logger.LogDebug("\"Fake helmet\" no longer casts a shadow");
            }

            if (__instance.playersManager.mapScreenPlayerName.text == "Player")
            {
                __instance.playersManager.mapScreenPlayerName.SetText(__instance.playersManager.mapScreen.radarTargets[__instance.playersManager.mapScreen.targetTransformIndex].name ?? "Player");
                Plugin.Logger.LogDebug("Fix \"MONITORING: Player\"");
            }

            GlobalReferences.viewmodelArms = __instance.thisPlayerModelArms;

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

            // in case the default suit has an attached costume object
            if (__instance.currentSuitID >= 0 && __instance.currentSuitID < __instance.playersManager.unlockablesList.unlockables.Count)
            {
                if (__instance.playersManager.unlockablesList.unlockables[__instance.currentSuitID].headCostumeObject != null || __instance.playersManager.unlockablesList.unlockables[__instance.currentSuitID].lowerTorsoCostumeObject != null)
                    UnlockableSuit.SwitchSuitForPlayer(__instance, __instance.currentSuitID, false);
            }
        }

        [HarmonyPatch(nameof(PlayerControllerB.PlayJumpAudio))]
        [HarmonyPostfix]
        static void PlayerControllerB_Post_PlayJumpAudio(PlayerControllerB __instance)
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

            if (!GlobalReferences.allEnemiesList.TryGetValue("MouthDog", out EnemyType mouthDog) || mouthDog.numberSpawned < 1)
                return;

            bool moving = false;
            if (__instance.IsOwner)
                moving = __instance.isWalking;
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

        [HarmonyPatch(nameof(PlayerControllerB.LandFromJumpClientRpc))]
        [HarmonyPostfix]
        static void PlayerControllerB_Post_LandFromJumpClientRpc(PlayerControllerB __instance)
        {
            if (!bunnyhoppingPlayers.Contains(__instance))
                return;

            Plugin.Logger.LogDebug($"Player \"{__instance.playerUsername}\" landed from bunnyhop");

            if (Configuration.fixJumpCheese.Value && __instance.IsServer)
            {
                if (GlobalReferences.allEnemiesList.TryGetValue("MouthDog", out EnemyType mouthDog) && mouthDog.numberSpawned > 0)
                    NonPatchFunctions.FakeFootstepAlert(__instance);
            }

            bunnyhoppingPlayers.Remove(__instance);
        }

        [HarmonyPatch(nameof(PlayerControllerB.PlayFootstepSound))]
        [HarmonyPostfix]
        static void PlayerControllerB_Post_PlayFootstepSound(PlayerControllerB __instance)
        {
            if (__instance.IsServer && !__instance.IsOwner && (int)__instance.actualClientId < NonPatchFunctions.playerWasLastSprinting.Length)
                NonPatchFunctions.playerWasLastSprinting[(int)__instance.actualClientId] = __instance.playerBodyAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Sprinting");
        }

        [HarmonyPatch(nameof(PlayerControllerB.QEItemInteract_performed))]
        [HarmonyPatch(nameof(PlayerControllerB.ItemSecondaryUse_performed))]
        [HarmonyPatch(nameof(PlayerControllerB.ItemTertiaryUse_performed))]
        [HarmonyPrefix]
        static void PlayerControllerB_Pre_Item_performed(PlayerControllerB __instance)
        {
            if (__instance.equippedUsableItemQE && __instance.currentlyHeldObjectServer != null && qeBugItems.Contains(__instance.currentlyHeldObjectServer.itemProperties.name))
            {
                __instance.equippedUsableItemQE = false;
                Plugin.Logger.LogWarning("Tried to use Q/E controls on an item with no secondary/tertiary use. This shouldn't happen");
            }
        }

        [HarmonyPatch(nameof(PlayerControllerB.PlaceGrabbableObject))]
        [HarmonyPostfix]
        static void PlayerControllerB_Post_PlaceGrabbableObject(GrabbableObject placeObject)
        {
            if (StartOfRound.Instance.isObjectAttachedToMagnet && StartOfRound.Instance.attachedVehicle != null && placeObject.transform.parent == StartOfRound.Instance.attachedVehicle.transform)
            {
                GameNetworkManager.Instance.localPlayerController.SetItemInElevator(true, true, placeObject);
                Plugin.Logger.LogDebug($"Item \"{placeObject.itemProperties.itemName}\" #{placeObject.GetInstanceID()} was placed inside a magnetized Cruiser and auto-collected");
            }

            // fix items shrinking/growing when dropped in elevator/cruiser
            Transform trans = placeObject.transform;
            Vector3 scalar = new(trans.localScale.x / trans.lossyScale.x, trans.localScale.y / trans.lossyScale.y, trans.localScale.z / trans.lossyScale.z);
            placeObject.transform.localScale = Vector3.Scale(placeObject.originalScale, scalar);
        }

        [HarmonyPatch(nameof(PlayerControllerB.PlayerLookInput))]
        [HarmonyPrefix]
        static void PlayerControllerB_Pre_PlayerLookInput(PlayerControllerB __instance, ref bool __state)
        {
            __state = __instance.disableLookInput;
            if (!__state && (GlobalReferences.lockingCamera > 0 || GlobalReferences.sittingInArmchair))
                __instance.disableLookInput = true;
        }

        [HarmonyPatch(nameof(PlayerControllerB.PlayerLookInput))]
        [HarmonyPostfix]
        static void PlayerControllerB_Post_PlayerLookInput(PlayerControllerB __instance, bool __state)
        {
            __instance.disableLookInput = __state;
        }

        [HarmonyPatch(nameof(PlayerControllerB.DiscardHeldObject))]
        [HarmonyPrefix]
        static void PlayerControllerB_Pre_DiscardHeldObject(PlayerControllerB __instance, bool placeObject, NetworkObject parentObjectTo)
        {
            if (!Compatibility.DISABLE_INTERACT_FIX && __instance.IsOwner && placeObject && parentObjectTo != null)
                __instance.throwingObject = true;
        }

        [HarmonyPatch(nameof(PlayerControllerB.SpawnDeadBody))]
        [HarmonyPrefix]
        static void PlayerControllerB_Pre_SpawnDeadBody(int playerId, int deathAnimation)
        {
            if (deathAnimation == 9)
            {
                if (GlobalReferences.gibbedPlayer != null)
                    Plugin.Logger.LogDebug("Two sets of Sapsucker gibs spawned simultaneously, this will likely cause them to look incorrect");

                GlobalReferences.gibbedPlayer = StartOfRound.Instance.allPlayerScripts[playerId];
            }
        }

        [HarmonyPatch(nameof(PlayerControllerB.ThrowObjectClientRpc))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> PlayerControllerB_Trans_ThrowObjectClientRpc(IEnumerable<CodeInstruction> instructions)
        {
            if (Compatibility.DISABLE_ROTATION_PATCH)
                return instructions;

            List<CodeInstruction> codes = instructions.ToList();

            MethodInfo setObjectAsNoLongerHeld = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.SetObjectAsNoLongerHeld));
            for (int i = 1; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && codes[i].operand as MethodInfo == setObjectAsNoLongerHeld && codes[i - 1].opcode == OpCodes.Ldc_I4_M1)
                {
                    codes[i - 1].opcode = OpCodes.Ldarg_S;
                    codes[i - 1].operand = (sbyte)5;
                    Plugin.Logger.LogDebug("Transpiler (Player drop): Preserve rotation");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Player drop transpiler failed");
            return instructions;
        }

        [HarmonyPatch(nameof(PlayerControllerB.SetObjectAsNoLongerHeld))]
        [HarmonyPostfix]
        static void PlayerControllerB_Post_SetObjectAsNoLongerHeld(PlayerControllerB __instance, int floorYRot)
        {
            Plugin.Logger.LogDebug($"Item dropped: {floorYRot}");
        }
    }
}