using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Rendering;

namespace ButteryFixes.Patches
{
    [HarmonyPatch]
    internal class GeneralPatches
    {
        internal static float MUSIC_DOPPLER_LEVEL = 1f;

        [HarmonyPatch(typeof(QuickMenuManager), "Start")]
        [HarmonyPostfix]
        static void QuickMenuManagerPostStart(QuickMenuManager __instance)
        {
            SpawnableEnemyWithRarity oldBird = __instance.testAllEnemiesLevel.OutsideEnemies.FirstOrDefault(enemy => enemy.enemyType.name == "RadMech");
            if (oldBird != null)
                oldBird.enemyType.requireNestObjectsToSpawn = true;
            Plugin.Logger.LogInfo("Old Birds now require \"nest\" to spawn");

            SpawnableEnemyWithRarity masked = __instance.testAllEnemiesLevel.Enemies.FirstOrDefault(enemy => enemy.enemyType.name == "MaskedPlayerEnemy");
            if (masked != null)
                masked.enemyType.isOutsideEnemy = false;
            Plugin.Logger.LogInfo("\"Masked\" now subtract from indoor power level");

            EnemyPatches.FLOWER_SNAKE = __instance.testAllEnemiesLevel.DaytimeEnemies.FirstOrDefault(enemy => enemy.enemyType.name == "FlowerSnake")?.enemyType;
        }

        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        static void StartOfRoundPostAwake(StartOfRound __instance)
        {
            SelectableLevel rend = __instance.levels.FirstOrDefault(level => level.name == "RendLevel");
            if (rend != null)
            {
                for (int i = 0; i < rend.spawnableMapObjects.Length; i++)
                {
                    if (rend.spawnableMapObjects[i].prefabToSpawn != null && rend.spawnableMapObjects[i].prefabToSpawn.name == "SpikeRoofTrapHazard")
                    {
                        rend.spawnableMapObjects[i].requireDistanceBetweenSpawns = true;
                        Plugin.Logger.LogInfo("Rend now properly spaces spike traps");
                    }
                }
            }

            GameObject helmetVisor = GameObject.Find("/Systems/Rendering/PlayerHUDHelmetModel/ScavengerHelmet");
            if (helmetVisor != null)
            {
                helmetVisor.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                Plugin.Logger.LogInfo("\"Fake helmet\" no longer casts a shadow");
            }

            switch (Plugin.configMusicDopplerLevel.Value)
            {
                case MusicDopplerLevel.None:
                    MUSIC_DOPPLER_LEVEL = 0f;
                    break;
                case MusicDopplerLevel.Reduced:
                    MUSIC_DOPPLER_LEVEL = 0.3f;
                    break;
                default:
                    MUSIC_DOPPLER_LEVEL = 1f;
                    break;
            }

            __instance.speakerAudioSource.dopplerLevel = MUSIC_DOPPLER_LEVEL;
            Plugin.Logger.LogInfo("Doppler level: Ship speaker");

            Dictionary<string, bool> conductiveItems = new()
            {
                //{ "Airhorn", true },
                //{ "DustPan", true },
                { "FancyCup", true },
                { "Flask", false },
                //{ "Hairdryer", true },
                { "MoldPan", true },
                //{ "Phone", true },
                { "Shotgun", true },
                { "SprayPaint", true },
                //{ "SteeringWheel", true }
            };
            foreach (Item item in __instance.allItemsList.itemsList)
            {
                switch (item.name)
                {
                    case "Knife":
                        item.spawnPrefab.GetComponent<KnifeItem>().SetScrapValue(35);
                        Plugin.Logger.LogInfo("Kitchen knives now display value on scan");
                        break;
                    case "Boombox":
                        item.spawnPrefab.GetComponent<BoomboxItem>().boomboxAudio.dopplerLevel = 0.3f * MUSIC_DOPPLER_LEVEL;
                        Plugin.Logger.LogInfo("Doppler level: Boombox");
                        break;
                    case "RadarBooster":
                    case "ExtensionLadder":
                        item.canBeInspected = false;
                        Plugin.Logger.LogInfo($"Inspectable: {item.itemName} (False)");
                        break;
                    case "MagnifyingGlass":
                    case "PillBottle":
                        item.canBeInspected = true;
                        Plugin.Logger.LogInfo($"Inspectable: {item.itemName} (True)");
                        break;
                }

                if (item.canBeInspected && item.toolTips.Length < 4)
                {
                    bool hasInspectTip = false;
                    foreach (string tooltip in item.toolTips)
                    {
                        if (tooltip.StartsWith("Inspect"))
                        {
                            hasInspectTip = true;
                            break;
                        }
                    }

                    if (!hasInspectTip)
                    {
                        item.toolTips.AddToArray("Inspect: [Z]");
                        Plugin.Logger.LogInfo($"Inspect tooltip: {item.itemName}");
                    }
                }

                if (conductiveItems.ContainsKey(item.name))
                {
                    item.isConductiveMetal = conductiveItems[item.name] && Plugin.configMakeConductive.Value;
                    Plugin.Logger.LogInfo($"Conductive: {item.itemName} ({item.isConductiveMetal})");
                }
            }

            // fix doppler level for furniture
            foreach (UnlockableItem unlockableItem in __instance.unlockablesList.unlockables)
            {
                switch (unlockableItem.unlockableName)
                {
                    /*case "Television":
                        unlockableItem.prefabObject.GetComponentInChildren<TVScript>().tvSFX.dopplerLevel = MUSIC_DOPPLER_LEVEL;
                        Plugin.Logger.LogInfo("Doppler level: Television");
                        break;*/
                    case "Record player":
                        unlockableItem.prefabObject.GetComponentInChildren<AnimatedObjectTrigger>().thisAudioSource.dopplerLevel = MUSIC_DOPPLER_LEVEL;
                        Plugin.Logger.LogInfo("Doppler level: Record player");
                        break;
                    case "Disco Ball":
                        unlockableItem.prefabObject.GetComponentInChildren<CozyLights>().turnOnAudio.dopplerLevel = 0.92f * MUSIC_DOPPLER_LEVEL;
                        Plugin.Logger.LogInfo("Doppler level: Disco ball");
                        break;
                }
            }
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
            music.GetComponent<AudioSource>().dopplerLevel = 0.6f * MUSIC_DOPPLER_LEVEL;
            music.Find("Music (1)").GetComponent<AudioSource>().dopplerLevel = 0.6f * MUSIC_DOPPLER_LEVEL;
            Plugin.Logger.LogInfo("Doppler level: Dropship");
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndPlayersFiredSequenceClientRpc))]
        [HarmonyPostfix]
        static void PostEndPlayersFiredSequenceClientRpc(StartOfRound __instance)
        {
            // reset TZP after firing sequence
            for (int i = 0; i < __instance.allPlayerScripts.Length; i++)
            {
                __instance.allPlayerScripts[i].drunkness = 0f;
                __instance.allPlayerScripts[i].drunknessInertia = 0f;
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
            if (!__instance.isHostingGame)
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
                        break;
                    }
                }
            }

            return codes;
        }
    }
}
