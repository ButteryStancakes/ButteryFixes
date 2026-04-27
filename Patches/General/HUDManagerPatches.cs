using ButteryFixes.Utility;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(HUDManager))]
    static class HUDManagerPatches
    {
        [HarmonyPatch(nameof(HUDManager.ShowPlayersFiredScreen))]
        [HarmonyPostfix]
        static void HUDManager_Post_ShowPlayersFiredScreen()
        {
            // reset TZP after firing sequence
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                StartOfRound.Instance.allPlayerScripts[i].drunkness = 0f;
                StartOfRound.Instance.allPlayerScripts[i].drunknessInertia = 0f;
            }
        }

        [HarmonyPatch(nameof(HUDManager.ApplyPenalty))]
        [HarmonyPostfix]
        static void HUDManager_Post_ApplyPenalty(HUDManager __instance, int playersDead, int bodiesInsured)
        {
            __instance.statsUIElements.penaltyAddition.SetText($"{playersDead} casualties: -{Mathf.Clamp((20 * (playersDead - bodiesInsured)) + (8 * bodiesInsured), 0, 100)}%\n({bodiesInsured} bodies recovered)");
        }

        [HarmonyPatch(nameof(HUDManager.GetNewStoryLogClientRpc))]
        [HarmonyPostfix]
        static void HUDManager_Post_GetNewStoryLogClientRpc(int logID)
        {
            foreach (StoryLog storyLog in Object.FindObjectsByType<StoryLog>(FindObjectsSortMode.None))
            {
                if (storyLog.storyLogID == logID)
                {
                    storyLog.CollectLog();
                    Plugin.Logger.LogInfo($"Another player collected data chip #{logID}");
                    return;
                }
            }
        }

        [HarmonyPatch(nameof(HUDManager.FillEndGameStats))]
        [HarmonyPostfix]
        static void HUDManager_Post_FillEndGameStats(HUDManager __instance, int scrapCollected = 0)
        {
            if (Compatibility.INSTALLED_GENERAL_IMPROVEMENTS || StartOfRound.Instance.allPlayersDead)
                return;

            if (RoundManager.Instance.totalScrapValueInLevel > ScrapTracker.TotalValue)
                Plugin.Logger.LogWarning("Vanilla is tracking more scrap than we are - this shouldn't happen");

            __instance.statsUIElements.quotaDenominator.SetText(ScrapTracker.TotalValue.ToString());
            ScrapTracker.Reset();

            /*int grade = 0;

            float scrapPercentage = (float)scrapCollected / ScrapTracker.TotalValue;
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
            __instance.statsUIElements.gradeLetter.SetText(grades[Mathf.Clamp(grade + 1, 0, grades.Length)]);*/
        }

        [HarmonyPatch(nameof(HUDManager.SetPlayerLevelSmoothly))]
        [HarmonyPrefix]
        static void HUDManager_Pre_SetPlayerLevelSmoothly(ref int XPGain)
        {
            // prevent NaN from decreasing XP all the way to 0
            if (XPGain < -8)
                XPGain = -8;
        }

        [HarmonyPatch(nameof(HUDManager.HelmetCondensationDrops))]
        [HarmonyPostfix]
        static void HUDManager_Post_HelmetCondensationDrops(HUDManager __instance)
        {
            if (!__instance.increaseHelmetCondensation && !TimeOfDay.Instance.insideLighting && TimeOfDay.Instance.effects[(int)LevelWeatherType.Flooded].effectEnabled && Vector3.Angle(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward, Vector3.up) < 45f)
                __instance.increaseHelmetCondensation = true;
        }

        [HarmonyPatch(nameof(HUDManager.CanPlayerScan))]
        [HarmonyPostfix]
        static void HUDManager_Post_CanPlayerScan(ref bool __result)
        {
            if (!__result && !Compatibility.DISABLE_SCAN_PATCH && GameNetworkManager.Instance.localPlayerController.inVehicleAnimation && !GameNetworkManager.Instance.localPlayerController.isPlayerDead)
                __result = true;
        }

        [HarmonyPatch(nameof(HUDManager.AddNewScrapFoundToDisplay))]
        [HarmonyPostfix]
        static void HUDManager_Post_AddNewScrapFoundToDisplay(HUDManager __instance)
        {
            if (__instance.itemsToBeDisplayed.Count < 1)
                return;

            GrabbableObject itemToDisplay = __instance.itemsToBeDisplayed[^1];
            if (itemToDisplay == null)
                return;

            if (itemToDisplay is GiftBoxItem giftBoxItem && giftBoxItem.deactivated)
                __instance.itemsToBeDisplayed.Remove(itemToDisplay);
        }
    }
}
