using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches
{
    [HarmonyPatch]
    internal class PlayerPatches
    {
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
            __instance.playerBadgeMesh.gameObject.SetActive(false);
            __instance.playerBetaBadgeMesh.gameObject.SetActive(false);
            Plugin.Logger.LogInfo("Hide badges on local player");
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
                    break;
                }
            }

            return codes;
        }
    }
}