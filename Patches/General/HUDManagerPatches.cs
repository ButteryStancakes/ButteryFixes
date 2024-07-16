using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch]
    internal class HUDManagerPatches
    {
        [HarmonyPatch(typeof(HUDManager), "UpdateScanNodes")]
        [HarmonyPostfix]
        static void PostUpdateScanNodes(HUDManager __instance, Dictionary<RectTransform, ScanNodeProperties> ___scanNodes)
        {
            if (!GlobalReferences.patchScanNodes || GameNetworkManager.Instance.localPlayerController == null)
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

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.ShowPlayersFiredScreen))]
        [HarmonyPostfix]
        static void PostShowPlayersFiredScreen()
        {
            // reset TZP after firing sequence
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                StartOfRound.Instance.allPlayerScripts[i].drunkness = 0f;
                StartOfRound.Instance.allPlayerScripts[i].drunknessInertia = 0f;
            }
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.ApplyPenalty))]
        [HarmonyPostfix]
        static void ApplyPenalty(HUDManager __instance, int playersDead, int bodiesInsured)
        {
            __instance.statsUIElements.penaltyAddition.SetText($"{playersDead} casualties: -{Mathf.Clamp((20 * (playersDead - bodiesInsured)) + (8 * bodiesInsured), 0, 100)}%\n({bodiesInsured} bodies recovered)");
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
                    Plugin.Logger.LogDebug("Transpiler (Health UI): Fix critical injury popup threshold");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Health UI transpiler failed");
            return codes;
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.GetNewStoryLogClientRpc))]
        [HarmonyPostfix]
        static void PostGetNewStoryLogClientRpc(int logID)
        {
            foreach (StoryLog storyLog in Object.FindObjectsOfType<StoryLog>())
            {
                if (storyLog.storyLogID == logID)
                {
                    storyLog.CollectLog();
                    Plugin.Logger.LogInfo($"Another player collected data chip #{logID}");
                    return;
                }
            }
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.FillEndGameStats))]
        [HarmonyPostfix]
        static void PostFillEndGameStats(HUDManager __instance, int scrapCollected = 0)
        {
            if (Compatibility.INSTALLED_GENERAL_IMPROVEMENTS)
                return;

            int scrapNotCollected = 0;
            foreach (GrabbableObject grabbableObject in Object.FindObjectsOfType<GrabbableObject>())
            {
                NetworkObject networkObject = grabbableObject.GetComponent<NetworkObject>();
                if (networkObject == null || !networkObject.IsSpawned)
                    continue;

                if (grabbableObject.itemProperties.isScrap && grabbableObject.scrapValue > 0 && ((grabbableObject is not GiftBoxItem && grabbableObject.deactivated) || (!grabbableObject.isInShipRoom && !grabbableObject.isInElevator && !grabbableObject.isHeld)) && !grabbableObject.scrapPersistedThroughRounds && grabbableObject is not RagdollGrabbableObject)
                    scrapNotCollected += grabbableObject.scrapValue;
            }

            __instance.statsUIElements.quotaDenominator.SetText((scrapCollected + scrapNotCollected).ToString());
        }
    }
}
