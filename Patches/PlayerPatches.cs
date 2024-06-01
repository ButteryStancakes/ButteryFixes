using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
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
            if (__instance.isClimbingLadder && !Plugin.DISABLE_LADDER_PATCH)
            {
                __instance.isSprinting = false;
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
        }

        [HarmonyPatch(typeof(HUDManager), "UpdateScanNodes")]
        [HarmonyPostfix]
        static void HUDManagerPostUpdateScanNodes(HUDManager __instance, Dictionary<RectTransform, ScanNodeProperties> ___scanNodes)
        {
            if (!Plugin.ENABLE_SCAN_PATCH || GameNetworkManager.Instance.localPlayerController == null)
                return;

            Rect rect = __instance.playerScreenTexture.GetComponent<RectTransform>().rect;
            for (int i = 0; i < __instance.scanElements.Length; i++)
            {
                if (___scanNodes.TryGetValue(__instance.scanElements[i], out ScanNodeProperties scanNodeProperties))
                {
                    Vector3 viewportPos = GameNetworkManager.Instance.localPlayerController.gameplayCamera.WorldToViewportPoint(scanNodeProperties.transform.position);
                    __instance.scanElements[i].anchoredPosition = new Vector2(rect.xMin + (rect.width * viewportPos.x), rect.yMin + (rect.height * viewportPos.y));
                }
            }
        }
    }
}