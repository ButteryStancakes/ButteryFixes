using ButteryFixes.Utility;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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
            if (!GlobalReferences.patchScanNodes || GameNetworkManager.Instance?.localPlayerController == null)
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
                    codes[i + 1].operand = (sbyte)10;
                    Plugin.Logger.LogDebug("Transpiler (Health UI): Fix critical injury popup threshold");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Health UI transpiler failed");
            return instructions;
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
            if (Compatibility.INSTALLED_GENERAL_IMPROVEMENTS || StartOfRound.Instance.allPlayersDead || GlobalReferences.scrapNotCollected < 0)
            {
                GlobalReferences.scrapEaten = 0;
                return;
            }

            int trueTotal = scrapCollected + GlobalReferences.scrapNotCollected + GlobalReferences.scrapEaten;
            GlobalReferences.scrapNotCollected = -1;
            GlobalReferences.scrapEaten = 0;

            __instance.statsUIElements.quotaDenominator.SetText(trueTotal.ToString());

            int grade = 0;

            float scrapPercentage = (float)scrapCollected / trueTotal;
            if (scrapPercentage >= 0.99f)
                grade += 2;
            else if (scrapPercentage >= 0.6f)
                grade++;
            else if (scrapPercentage <= 0.25f)
                grade--;

            int numPlayers = 0;
            int livingPlayers = 0;
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if ((player.isPlayerControlled || player.isPlayerDead) && !player.disconnectedMidGame)
                {
                    numPlayers++;
                    if (!player.isPlayerDead)
                        livingPlayers++;
                }
            }
            Plugin.Logger.LogDebug($"End-of-round: {livingPlayers}/{numPlayers} survived");
            if (numPlayers == livingPlayers)
                grade++;
            else if ((numPlayers - livingPlayers) > 1)
                grade--;

            string[] grades = { "D", "C", "B", "A", "S" };
            __instance.statsUIElements.gradeLetter.SetText(grades[Mathf.Clamp(grade + 1, 0, grades.Length)]);
        }

        [HarmonyPatch(typeof(HUDManager), "SetPlayerLevelSmoothly")]
        [HarmonyPrefix]
        static void PreSetPlayerLevelSmoothly(ref int XPGain)
        {
            // prevent NaN from decreasing XP all the way to 0
            if (XPGain < -8)
                XPGain = -8;
        }

        [HarmonyPatch(typeof(HUDManager), "HelmetCondensationDrops")]
        [HarmonyPostfix]
        static void PostHelmetCondensationDrops(HUDManager __instance)
        {
            if (!__instance.increaseHelmetCondensation && !TimeOfDay.Instance.insideLighting && TimeOfDay.Instance.effects[(int)LevelWeatherType.Flooded].effectEnabled && Vector3.Angle(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward, Vector3.up) < 45f)
                __instance.increaseHelmetCondensation = true;
        }

        [HarmonyPatch(typeof(HUDManager), "CanPlayerScan")]
        [HarmonyPostfix]
        static void Post_CanPlayerScan(ref bool __result)
        {
            if (!__result && GameNetworkManager.Instance.localPlayerController.inVehicleAnimation && !GameNetworkManager.Instance.localPlayerController.isPlayerDead)
                __result = true;
        }
    }
}
