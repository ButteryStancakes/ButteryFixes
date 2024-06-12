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
        static List<PlayerControllerB> bunnyhoppingPlayers = new();

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
            Renderer scavengerHelmet = __instance.localVisor.Find("ScavengerHelmet")?.GetComponent<Renderer>();
            if (scavengerHelmet != null)
            {
                scavengerHelmet.shadowCastingMode = ShadowCastingMode.Off;
                Plugin.Logger.LogInfo("\"Fake helmet\" no longer casts a shadow");
            }
            try
            {
                __instance.playerBadgeMesh.GetComponent<Renderer>().forceRenderingOff = true;
                __instance.playerBetaBadgeMesh.forceRenderingOff = true;
                Plugin.Logger.LogInfo("Hide badges on local player");
            }
            catch (System.Exception e)
            {
                Plugin.Logger.LogError("Ran into error fetching local player's badges");
                Plugin.Logger.LogError(e);
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
            if (!__instance.IsOwner && !HUDManager.Instance.itemSlotIcons[itemSlot].enabled && GameNetworkManager.Instance.localPlayerController.ItemSlots[itemSlot] != null)
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
    }
}