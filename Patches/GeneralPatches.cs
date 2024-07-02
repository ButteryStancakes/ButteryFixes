using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches
{
    [HarmonyPatch]
    internal class GeneralPatches
    {
        [HarmonyPatch(typeof(QuickMenuManager), "Start")]
        [HarmonyPostfix]
        static void QuickMenuManagerPostStart(QuickMenuManager __instance)
        {
            // cache all references to enemy types
            GlobalReferences.allEnemiesList.Clear();
            List<SpawnableEnemyWithRarity>[] allEnemyLists =
            [
                __instance.testAllEnemiesLevel.Enemies,
                __instance.testAllEnemiesLevel.OutsideEnemies,
                __instance.testAllEnemiesLevel.DaytimeEnemies
            ];

            foreach (List<SpawnableEnemyWithRarity> enemies in allEnemyLists)
                foreach (SpawnableEnemyWithRarity spawnableEnemyWithRarity in enemies)
                {
                    if (GlobalReferences.allEnemiesList.ContainsKey(spawnableEnemyWithRarity.enemyType.name))
                    {
                        if (GlobalReferences.allEnemiesList[spawnableEnemyWithRarity.enemyType.name] == spawnableEnemyWithRarity.enemyType)
                            Plugin.Logger.LogWarning($"allEnemiesList: Tried to cache reference to \"{spawnableEnemyWithRarity.enemyType.name}\" more than once");
                        else
                            Plugin.Logger.LogWarning($"allEnemiesList: Tried to cache two different enemies by same name ({spawnableEnemyWithRarity.enemyType.name})");
                    }
                    else
                        GlobalReferences.allEnemiesList.Add(spawnableEnemyWithRarity.enemyType.name, spawnableEnemyWithRarity.enemyType);
                }

            ScriptableObjectOverrides.OverrideEnemyTypes();
        }

        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        static void StartOfRoundPostAwake(StartOfRound __instance)
        {
            ScriptableObjectOverrides.OverrideSelectableLevels();

            GlobalReferences.dopplerLevelMult = Plugin.configMusicDopplerLevel.Value switch
            {
                MusicDopplerLevel.None => 0f,
                MusicDopplerLevel.Reduced => 0.333f,
                _ => 1f,
            };
            __instance.speakerAudioSource.dopplerLevel = GlobalReferences.dopplerLevelMult;
            Plugin.Logger.LogInfo("Doppler level: Ship speaker");

            GameObject tragedyRagdoll = StartOfRound.Instance.playerRagdolls.FirstOrDefault(playerRagdoll => playerRagdoll.name == "PlayerRagdollWithTragedyMask Variant");
            if (tragedyRagdoll != null)
            {
                // cache all of the visual references to the tragedy mask (the item and enemy prefabs are broken, only the ragdoll has all the correct assets)
                foreach (MeshFilter meshFilter in tragedyRagdoll.GetComponentsInChildren<MeshFilter>())
                {
                    switch (meshFilter.name)
                    {
                        case "Mesh":
                            GlobalReferences.tragedyMask = meshFilter.sharedMesh;
                            GlobalReferences.tragedyMaskMat = meshFilter.GetComponent<MeshRenderer>()?.sharedMaterial;
                            break;
                        case "ComedyMaskLOD1":
                            GlobalReferences.tragedyMaskLOD = meshFilter.sharedMesh;
                            break;
                        case "EyesFilled":
                            GlobalReferences.tragedyMaskEyesFilled = meshFilter.sharedMesh;
                            break;
                    }
                }
            }

            GlobalReferences.playerBody = StartOfRound.Instance.playerRagdolls[0].GetComponent<SkinnedMeshRenderer>().sharedMesh;
            GlobalReferences.scavengerSuitBurnt = StartOfRound.Instance.playerRagdolls[6].GetComponent<SkinnedMeshRenderer>().sharedMaterial;

            ScriptableObjectOverrides.OverrideItems();
            AudioSource stickyNote = __instance.elevatorTransform.Find("StickyNoteItem")?.GetComponent<AudioSource>();
            if (stickyNote != null)
            {
                stickyNote.rolloffMode = AudioRolloffMode.Linear;
                stickyNote.GetComponent<PhysicsProp>().scrapValue = 0;
                Plugin.Logger.LogInfo($"Audio rolloff: Sticky note");
            }
            AudioSource clipboard = __instance.elevatorTransform.Find("ClipboardManual")?.GetComponent<AudioSource>();
            if (clipboard != null)
            {
                clipboard.rolloffMode = AudioRolloffMode.Linear;
                clipboard.GetComponent<ClipboardItem>().scrapValue = 0;
                Plugin.Logger.LogInfo($"Audio rolloff: Clipboard");
            }

            ScriptableObjectOverrides.OverrideUnlockables();
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetStats))]
        [HarmonyPostfix]
        static void StartOfRoundPostResetStats(StartOfRound __instance)
        {
            // stop tracking "most profitable" between days
            for (int i = 0; i < __instance.gameStats.allPlayerStats.Length; i++)
                __instance.gameStats.allPlayerStats[i].profitable = 0;
            Plugin.Logger.LogInfo("Cleared \"profitable\" stat for all employees");
        }

        [HarmonyPatch(typeof(StartOfRound), "ResetShipFurniture")]
        [HarmonyPostfix]
        static void PostResetShipFurniture(StartOfRound __instance)
        {
            if (__instance.IsServer)
            {
                Terminal terminal = Object.FindObjectOfType<Terminal>();
                // empty the dropship on game over
                if (terminal != null && terminal.orderedItemsFromTerminal.Count > 0)
                {
                    terminal.orderedItemsFromTerminal.Clear();
                    terminal.SyncGroupCreditsServerRpc(terminal.groupCredits, 0);
                    Plugin.Logger.LogInfo("Dropship inventory was emptied (game over)");
                }
            }
            // reset TZP between challenge moon attempts
            if (__instance.isChallengeFile)
            {
                for (int i = 0; i < __instance.allPlayerScripts.Length; i++)
                {
                    __instance.allPlayerScripts[i].drunkness = 0f;
                    __instance.allPlayerScripts[i].drunknessInertia = 0f;
                }
            }
        }

        [HarmonyPatch(typeof(ItemDropship), "Start")]
        [HarmonyPostfix]
        static void ItemDropshipPostStart(ItemDropship __instance)
        {
            // fix doppler level for dropship (both music sources)
            Transform music = __instance.transform.Find("Music");
            if (music != null)
            {
                music.GetComponent<AudioSource>().dopplerLevel = 0.6f * GlobalReferences.dopplerLevelMult;
                AudioSource musicFar = music.Find("Music (1)")?.GetComponent<AudioSource>();
                if (musicFar != null)
                    musicFar.dopplerLevel = 0.6f * GlobalReferences.dopplerLevelMult;
                Plugin.Logger.LogInfo("Doppler level: Dropship");
            }
            // honestly just leave the vehicle version as-is, it's funny
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

        [HarmonyPatch(typeof(StartOfRound), "LoadShipGrabbableItems")]
        [HarmonyPostfix]
        static void PostLoadShipGrabbableItems()
        {
            Terminal terminal = Object.FindObjectOfType<Terminal>();
            // reload the dropship's contents from the save file, if any exist
            if (terminal != null)
            {
                try
                {
                    terminal.orderedItemsFromTerminal = ES3.Load("ButteryFixes_DeliveryItems", GameNetworkManager.Instance.currentSaveFileName, new List<int>());
                    terminal.numberOfItemsInDropship = terminal.orderedItemsFromTerminal.Count;
                    if (terminal.numberOfItemsInDropship > 0)
                    {
                        Plugin.Logger.LogInfo($"Dropship inventory was restocked from save file ({terminal.numberOfItemsInDropship} items):");
                        for (int i = 0; i < terminal.numberOfItemsInDropship; i++)
                            Plugin.Logger.LogInfo($"#{i + 1} - {terminal.buyableItemsList[terminal.orderedItemsFromTerminal[i]].itemName}");
                    }
                }
                catch (System.Exception e)
                {
                    Plugin.Logger.LogError($"An error occurred while fetching dropship inventory from save file \"{GameNetworkManager.Instance.currentSaveFileName}\"");
                    Plugin.Logger.LogError(e);
                }
            }
        }

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.SaveGame))]
        [HarmonyPostfix]
        static void PostSaveGame(GameNetworkManager __instance)
        {
            if (!__instance.isHostingGame || StartOfRound.Instance.isChallengeFile || !StartOfRound.Instance.inShipPhase)
                return;

            Terminal terminal = Object.FindObjectOfType<Terminal>();
            if (terminal != null)
            {
                try
                {
                    ES3.Save("ButteryFixes_DeliveryItems", terminal.orderedItemsFromTerminal, __instance.currentSaveFileName);
                    Plugin.Logger.LogInfo($"Dropship inventory saved ({terminal.numberOfItemsInDropship} items)");
                }
                catch (System.Exception e)
                {
                    Plugin.Logger.LogError($"An error occurred while trying to save dropship inventory to file \"{GameNetworkManager.Instance.currentSaveFileName}\"");
                    Plugin.Logger.LogError(e);
                }
            }
        }

        [HarmonyPatch(typeof(Terminal), "Start")]
        [HarmonyPostfix]
        static void TerminalPostStart(Terminal __instance)
        {
            foreach (TerminalNode enemyFile in __instance.enemyFiles)
            {
                switch (enemyFile.name)
                {
                    case "NutcrackerFile":
                        if (enemyFile.displayText.EndsWith("house."))
                        {
                            enemyFile.displayText += "\n\nThey watch with one tireless eye, which only senses movement; It remembers the last creature it noticed whether they are moving or not.";
                            Plugin.Logger.LogInfo("Bestiary: Nutcracker");
                        }
                        break;
                    case "RadMechFile":
                        enemyFile.displayText = enemyFile.displayText.Replace("\n The subject of who developed the Old Birds has been an intense debate since their first recorded appearance on", "");
                        Plugin.Logger.LogInfo("Bestiary: Old Birds");
                        break;
                    case "MaskHornetsFile":
                        enemyFile.creatureName = enemyFile.creatureName[0].ToString().ToUpper() + enemyFile.creatureName[1..];
                        Plugin.Logger.LogInfo("Bestiary: Mask hornets");
                        break;
                }
            }
        }

        [HarmonyPatch(typeof(Terminal), "TextPostProcess")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> TransTextPostProcess(IEnumerable<CodeInstruction> instructions)
        {
            if (Plugin.GENERAL_IMPROVEMENTS)
                return instructions;

            List<CodeInstruction> codes = instructions.ToList();

            for (int i = codes.Count - 1; i >= 0; i--)
            {
                if (codes[i].opcode == OpCodes.Ldstr)
                {
                    string str = (string)codes[i].operand;
                    if (str.Contains("\nn"))
                    {
                        codes[i].operand = str.Replace("\nn", "\n");
                        Plugin.Logger.LogDebug("Transpiler: Fix \"n\" on terminal when viewing monitor");
                        return codes;
                    }
                }
            }

            Plugin.Logger.LogDebug("Terminal transpiler failed");
            return codes;
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.ApplyPenalty))]
        [HarmonyPostfix]
        static void ApplyPenalty(HUDManager __instance, int playersDead, int bodiesInsured)
        {
            /*float fine = 100;
            fine *= Mathf.Pow(0.8f, playersDead - bodiesInsured);
            fine *= Mathf.Pow(0.92f, bodiesInsured);
            fine = Mathf.FloorToInt(100 - fine);
            __instance.statsUIElements.penaltyAddition.text = $"{playersDead} casualties: -{fine}%\n({bodiesInsured} bodies recovered)";*/
            __instance.statsUIElements.penaltyAddition.text = $"{playersDead} casualties: -{Mathf.Clamp((20 * (playersDead - bodiesInsured)) + (8 * bodiesInsured), 0, 100)}%\n({bodiesInsured} bodies recovered)";
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.PredictAllOutsideEnemies))]
        [HarmonyPostfix]
        static void PostPredictAllOutsideEnemies()
        {
            // cleans up leftover spawn numbers from previous day + spawn predictions (when generating nests)
            foreach (string name in GlobalReferences.allEnemiesList.Keys)
                GlobalReferences.allEnemiesList[name].numberSpawned = 0;
        }

        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Disconnect))]
        [HarmonyPostfix]
        static void GameNetworkManagerPostDisconnect()
        {
            GlobalReferences.allEnemiesList.Clear();
        }

        [HarmonyPatch(typeof(RoundManager), "Start")]
        [HarmonyPostfix]
        static void RoundManagerPostStart(RoundManager __instance)
        {
            __instance.ResetEnemyVariables();
        }

        [HarmonyPatch(typeof(BreakerBox), nameof(BreakerBox.SetSwitchesOff))]
        [HarmonyPostfix]
        static void PostSetSwitchesOff(BreakerBox __instance)
        {
            __instance.breakerBoxHum.Stop();
        }

        // is it a bug that the breaker box hums after unplugging the apparatus? I'm not too sure
        /*[HarmonyPatch(typeof(RoundManager), nameof(RoundManager.PowerSwitchOffClientRpc))]
        [HarmonyPostfix]
        static void PostPowerSwitchOffClientRpc()
        {
            Object.FindObjectOfType<BreakerBox>()?.breakerBoxHum.Stop();
        }
        
        [HarmonyPatch(typeof(BreakerBox), nameof(BreakerBox.SwitchBreaker))]
        [HarmonyPostfix]
        static void PostSwitchBreaker(BreakerBox __instance)
        {
            if (__instance.breakerBoxHum.isPlaying && RoundManager.Instance.powerOffPermanently)
                __instance.breakerBoxHum.Stop();
        }*/
    }
}
