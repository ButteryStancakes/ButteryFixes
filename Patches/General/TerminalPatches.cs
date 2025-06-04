using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalPatches
    {
        static int groupCreditsLastFrame = -1;
        static RectTransform groupCreditsBackground;
        static TerminalKeyword gordion = null;
        static readonly Dictionary<string, (int, int)> valueOverrides = new()
        {
            { "Knife", (28, 84) },
            { "RedLocustHive", (40, 150) },
            { "Shotgun", (25, 90) }, // 25 from v50 beta
            { "LungApparatus", (40, 131) }, // 130 from v9
            { "BabyKiwiEgg", (30, 200) },
        };

        [HarmonyPatch(nameof(Terminal.Start))]
        [HarmonyPostfix]
        static void Terminal_Post_Start(Terminal __instance)
        {
            if (!Compatibility.ENABLE_VAIN_SHROUDS)
            {
                TerminalNode vainShroudFile = __instance.enemyFiles.FirstOrDefault(enemyFile => enemyFile.name == "VainShroudFile");
                if (vainShroudFile != null && __instance.scannedEnemyIDs.Contains(vainShroudFile.creatureFileID))
                {
                    Compatibility.ENABLE_VAIN_SHROUDS = true;
                    Plugin.Logger.LogDebug("Vain Shrouds have already been scanned on this save");
                }
            }

            foreach (TerminalNode enemyFile in __instance.enemyFiles)
            {
                switch (enemyFile.name)
                {
                    case "NutcrackerFile":
                        if (enemyFile.displayText.EndsWith("house."))
                        {
                            enemyFile.displayText += "\n\nThey watch with one tireless eye, which only senses movement; It remembers the last creature it noticed whether they are moving or not.";
                            Plugin.Logger.LogDebug("Bestiary: Nutcracker");
                        }
                        break;
                    case "RadMechFile":
                        if (!enemyFile.displayText.Contains("Anglen"))
                        {
                            enemyFile.displayText = enemyFile.displayText.Replace(
                                "\n The subject of who developed the Old Birds has been an intense debate since their first recorded appearance on December 18 of 2143, when a large number of Old Birds invaded the",
                                "\n The subject of who developed the Old Birds has been an intense debate since their first recorded appearance on December 18 of 2143, when over fifty Old Birds invaded the Anglen Capital. This is considered one of the first major causes for the downfall of the Anglen Empire. The most commonly upheld theory takes into account the tension between the Anglen and Buemoch military throughout the 2100's, however nothing has been proven in the centuries since.");
                        }
                        if (!enemyFile.displayText.Contains("Sigurd") && Compatibility.ENABLE_VAIN_SHROUDS)
                        {
                            enemyFile.displayText = enemyFile.displayText.Replace("OLD BIRDS", "OLD BIRDS\n\nSigurd's danger level: 95%")
                                + " DON'T MESS AROUND OR THEYLL GIVE YOU A RIDE.tHEY LOSE TRACK QUICK AND THEY CANT TURN VERY FAST, THEYRE DUMB AND THEY WONT SHUT UP sorry caps";
                        }
                        Plugin.Logger.LogDebug("Bestiary: Old Birds");
                        break;
                    case "MaskHornetsFile":
                        enemyFile.creatureName = enemyFile.creatureName[0].ToString().ToUpper() + enemyFile.creatureName[1..];
                        Plugin.Logger.LogDebug("Bestiary: Mask hornets");
                        break;
                }
            }

            TerminalKeyword buy = __instance.terminalNodes.allKeywords.FirstOrDefault(keyword => keyword.name == "Buy");

            BuyableVehicle cruiser = __instance.buyableVehicles.FirstOrDefault(buyableVehicle => buyableVehicle.vehicleDisplayName == "Cruiser");
            if (cruiser != null)
            {
                if (buy != null)
                {
                    TerminalNode buyCruiser = buy.compatibleNouns.FirstOrDefault(noun => noun.noun.name == "Cruiser")?.result;
                    if (buyCruiser != null)
                    {
                        // fix cruiser price shown as $400 after price buff
                        cruiser.creditsWorth = buyCruiser.itemCost;
                        Plugin.Logger.LogDebug("Price: Cruiser");
                    }
                }

                AudioSource clipboardCruiser = cruiser.secondaryPrefab?.transform.GetComponent<AudioSource>();
                if (clipboardCruiser != null)
                {
                    clipboardCruiser.rolloffMode = AudioRolloffMode.Linear;
                    Plugin.Logger.LogDebug("Audio rolloff: Clipboard (Cruiser)");
                }
            }

            if (buy != null)
            {
                TerminalNode buyWelcomeMat = buy.compatibleNouns.FirstOrDefault(noun => noun.noun?.name == "WelcomeMat")?.result?.terminalOptions?.FirstOrDefault(option => option.noun?.name == "Confirm")?.result;
                if (buyWelcomeMat != null)
                {
                    buyWelcomeMat.itemCost = 40;
                    Plugin.Logger.LogDebug("Price: Welcome mat");
                }
            }

            if (!Compatibility.DISABLE_PRICE_TEXT_FITTING)
            {
                groupCreditsLastFrame = -1;
                if (groupCreditsBackground == null)
                {
                    foreach (Transform child in __instance.topRightText.transform.parent)
                    {
                        if (child.name == "Image" && child.TryGetComponent(out Image image) && image.enabled)
                        {
                            groupCreditsBackground = (RectTransform)child;
                            break;
                        }
                    }
                }
            }

            if (Configuration.typeGordion.Value)
            {
                if (!__instance.terminalNodes.allKeywords.Any(keyword => keyword.word == "gordion"))
                {
                    TerminalKeyword companyMoon = __instance.terminalNodes.allKeywords.FirstOrDefault(keyword => keyword.word == "company");
                    if (companyMoon != null)
                    {
                        TerminalKeyword route = companyMoon.defaultVerb;
                        if (route != null)
                        {
                            if (gordion == null)
                            {
                                gordion = (TerminalKeyword)ScriptableObject.CreateInstance(typeof(TerminalKeyword));
                                gordion.word = "gordion";
                                gordion.defaultVerb = route;
                            }

                            CompatibleNoun companyNoun = route.compatibleNouns.FirstOrDefault(compatibleNoun => compatibleNoun.noun == companyMoon);
                            if (companyNoun != null)
                            {
                                route.compatibleNouns =
                                [
                                    .. route.compatibleNouns,
                                    new()
                                    {
                                        noun = gordion,
                                        result = companyNoun.result
                                    },
                                ];
                                __instance.terminalNodes.allKeywords =
                                [
                                    .. __instance.terminalNodes.allKeywords,
                                    gordion
                                ];

                                Plugin.Logger.LogDebug("Terminal: Registered \"gordion\" to route");

                                TerminalKeyword info = __instance.terminalNodes.allKeywords.FirstOrDefault(keyword => keyword.word == "info");
                                if (info != null)
                                {
                                    CompatibleNoun companyNoun2 = info.compatibleNouns.FirstOrDefault(compatibleNoun => compatibleNoun.noun == companyMoon);
                                    if (companyNoun2 != null)
                                    {
                                        info.compatibleNouns =
                                        [
                                            .. info.compatibleNouns,
                                            new()
                                            {
                                                noun = gordion,
                                                result = companyNoun2.result
                                            },
                                        ];

                                        Plugin.Logger.LogDebug("Terminal: Registered \"gordion\" to info");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            TerminalNode shipCosmeticsList = __instance.terminalNodes.allKeywords.FirstOrDefault(keyword => keyword.name == "Upgrades")?.specialKeywordResult;
            if (shipCosmeticsList != null)
            {
                shipCosmeticsList.displayText = shipCosmeticsList.displayText.Replace("* Teleporter      //    Price: $400", "* Loud horn      //    Price: $100\n* Signal Translator      //    Price: $255\n* Teleporter      //    Price: $375\n* Inverse Teleporter      //    Price: $425");
                Plugin.Logger.LogDebug("Terminal: Upgrades");
            }
        }

        [HarmonyPatch(nameof(Terminal.TextPostProcess))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.VeryHigh)]
        [HarmonyBefore(Compatibility.GUID_LETHAL_FIXES)]
        static void Terminal_Pre_TextPostProcess(Terminal __instance, ref string modifiedDisplayText)
        {
            if (Configuration.scanImprovements.Value && modifiedDisplayText.Contains("[scanForItems]"))
            {
                bool scanOnShip = StartOfRound.Instance.inShipPhase || (GlobalReferences.HangarShipDoor != null && !GlobalReferences.HangarShipDoor.buttonsEnabled) || StartOfRound.Instance.currentLevel.name == "CompanyBuildingLevel";

                int objects = 0;
                int value = 0;
                System.Random rand = new(StartOfRound.Instance.randomMapSeed + 91);
                string vehicleText = string.Empty;
                foreach (GrabbableObject grabbableObject in Object.FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None))
                {
                    if (!grabbableObject.itemProperties.isScrap || grabbableObject is RagdollGrabbableObject)
                        continue;

                    bool inShip = grabbableObject.isInShipRoom || grabbableObject.isInElevator;
                    bool inCruiser = scanOnShip && StartOfRound.Instance.isObjectAttachedToMagnet && GlobalReferences.vehicleController != null && grabbableObject.transform.parent == GlobalReferences.vehicleController.transform;

                    if (inShip == scanOnShip || inCruiser)
                    {
                        if (scanOnShip || objects < 1)
                            value += grabbableObject.scrapValue;
                        else
                        {
                            int min = grabbableObject.itemProperties.minValue, max = grabbableObject.itemProperties.maxValue;
                            if (valueOverrides.TryGetValue(grabbableObject.itemProperties.name, out (int realMin, int realMax) values))
                            {
                                min = values.realMin;
                                max = values.realMax;
                            }
                            else
                            {
                                min = (int)(min * RoundManager.Instance.scrapValueMultiplier);
                                max = (int)(max * RoundManager.Instance.scrapValueMultiplier);
                            }

                            value += Mathf.Clamp(rand.Next(min, max), grabbableObject.scrapValue - (6 * objects), grabbableObject.scrapValue + (9 * objects));
                        }
                        objects++;

                        if (inCruiser && string.IsNullOrEmpty(vehicleText))
                            vehicleText = " and Cruiser";
                    }
                }

                if (scanOnShip)
                    modifiedDisplayText = modifiedDisplayText.Replace("[scanForItems]", $"There are {objects} objects inside the ship{vehicleText}, totalling at an exact value of ${value}.");
                else
                {
                    // also count unkilled butlers
                    for (int i = 0; i < ButlerRadar.CountButlers(); i++)
                    {
                        (int min, int max) = valueOverrides["Knife"];
                        value += Mathf.Clamp(rand.Next(min, max), 35 - (6 * objects), 35 + (9 * objects));
                        objects++;
                    }

                    modifiedDisplayText = modifiedDisplayText.Replace("[scanForItems]", $"There are {objects} objects outside the ship, totalling at an approximate value of ${value}.");
                }
            }

            if (modifiedDisplayText.Contains("[numberOfItemsOnRoute]") && __instance.vehicleInDropship)
                modifiedDisplayText = modifiedDisplayText.Replace("[numberOfItemsOnRoute]", "1 purchased vehicle on route.");

            if (Configuration.filterDecor.Value && modifiedDisplayText.Contains("[unlockablesSelectionList]") && __instance.ShipDecorSelection != null)
            {
                TerminalNode[] filteredDecor = __instance.ShipDecorSelection.Where(node => !StartOfRound.Instance.unlockablesList.unlockables[node.shipUnlockableID].hasBeenUnlockedByPlayer).ToArray();
                if (filteredDecor.Length < 1)
                    modifiedDisplayText = modifiedDisplayText.Replace("[unlockablesSelectionList]", "[No items available]");
                else
                {
                    string allDecor = string.Empty;
                    foreach (TerminalNode decor in filteredDecor)
                        allDecor += $"\n{decor.creatureName}  //  ${decor.itemCost}";
                    modifiedDisplayText = modifiedDisplayText.Replace("[unlockablesSelectionList]", allDecor);
                }
            }
        }

        [HarmonyPatch(nameof(Terminal.ParsePlayerSentence))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Terminal_Trans_ParsePlayerSentence(IEnumerable<CodeInstruction> instructions)
        {
            if (Compatibility.INSTALLED_GENERAL_IMPROVEMENTS)
                return instructions;

            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo playerDefinedAmount = AccessTools.Field(typeof(Terminal), nameof(Terminal.playerDefinedAmount));
            MethodInfo clamp = AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), [typeof(int), typeof(int), typeof(int)]);
            for (int i = 2; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Stfld && (FieldInfo)codes[i].operand == playerDefinedAmount && codes[i - 1].opcode == OpCodes.Call && codes[i - 1].operand as MethodInfo == clamp && codes[i - 2].opcode == OpCodes.Ldc_I4_S)
                {
                    codes[i - 2].operand = (sbyte)12;
                    Plugin.Logger.LogDebug("Transpiler (Order capacity): Allow bulk purchases of 12");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Order capacity transpiler failed");
            return instructions;
        }

        [HarmonyPatch(nameof(Terminal.LoadNewNodeIfAffordable))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Terminal_Trans_LoadNewNodeIfAffordable(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            //FieldInfo buyItemIndex = AccessTools.Field(typeof(TerminalNode), nameof(TerminalNode.buyItemIndex));
            //FieldInfo numberOfItemsInDropship = AccessTools.Field(nameof(Terminal.numberOfItemsInDropship));
            bool /*inSurvivalKit = false,*/ walkies = false, shovel = false;
            for (int i = 1; i < codes.Count - 1; i++)
            {
                /*if (inSurvivalKit)
                {
                    // went too far, transpiler failed
                    if (codes[i].opcode == OpCodes.Stfld && (FieldInfo)codes[i].operand == numberOfItemsInDropship)
                        break;*/

                if (codes[i].opcode == OpCodes.Callvirt)
                {
                    string methodName = codes[i].ToString();
                    if (methodName.Contains("System.Collections.Generic.List") && methodName.Contains("Add"))
                    {
                        if (shovel && walkies)
                            return codes;

                        if (!shovel && codes[i - 1].opcode == OpCodes.Ldc_I4_5)
                        {
                            shovel = true;
                            codes[i - 1].opcode = OpCodes.Ldc_I4_2;
                            Plugin.Logger.LogDebug("Transpiler (Survival kit): Stun grenade -> shovel");
                        }
                        else if (!walkies && codes[i - 1].opcode == OpCodes.Ldc_I4_6)
                        {
                            walkies = true;
                            codes[i - 1].opcode = OpCodes.Ldc_I4_0;
                            Plugin.Logger.LogDebug("Transpiler (Survival kit): Boomboxes -> walkie-talkies");
                        }
                    }
                }
                /*}
                // need to make sure this skips to to the correct occurrence
                else if (codes[i].opcode == OpCodes.Ldfld && (FieldInfo)codes[i].operand == buyItemIndex && codes[i + 1].opcode == OpCodes.Ldc_I4_S && (sbyte)codes[i + 1].operand == -7)
                    inSurvivalKit = true;*/
            }

            Plugin.Logger.LogError("Survival kit transpiler failed");
            return instructions;
        }

        [HarmonyPatch(nameof(Terminal.BeginUsingTerminal))]
        [HarmonyPostfix]
        static void Terminal_Post_BeginUsingTerminal(Terminal __instance)
        {
            if (Configuration.lockInTerminal.Value)
            {
                GlobalReferences.lockingCamera++;
                // enable screen immediately (normally delayed by 1-2 frames)
                __instance.terminalUIScreen.gameObject.SetActive(true);
                // select text field immediately (normally delayed 1s)
                __instance.screenText.ActivateInputField();
                __instance.screenText.Select();
            }
        }

        [HarmonyPatch(nameof(Terminal.QuitTerminal))]
        [HarmonyPrefix]
        static void Terminal_Pre_QuitTerminal(Terminal __instance)
        {
            if (GlobalReferences.lockingCamera > 0 && GameNetworkManager.Instance.localPlayerController.inTerminalMenu)
                GlobalReferences.lockingCamera--;
        }

        [HarmonyPatch(nameof(Terminal.Update))]
        [HarmonyPostfix]
        static void Terminal_Post_Update(Terminal __instance)
        {
            if (!Compatibility.DISABLE_PRICE_TEXT_FITTING && __instance.terminalInUse && groupCreditsLastFrame != __instance.groupCredits && __instance.terminalUIScreen.gameObject.activeSelf && __instance.topRightText.isActiveAndEnabled)
            {
                groupCreditsLastFrame = __instance.groupCredits;

                // resize text box to fit more digits
                if (groupCreditsBackground != null)
                {
                    float extraWidth = Mathf.Clamp(__instance.topRightText.preferredWidth - 88.4f, 0f, 75.08f);
                    //Plugin.Logger.LogDebug($"Terminal text: {__instance.topRightText.text}");
                    //Plugin.Logger.LogDebug($"Terminal text preferred width: {__instance.topRightText.preferredWidth}");
                    if (groupCreditsBackground.sizeDelta.x != (75.175f + extraWidth))
                    {
                        groupCreditsBackground.localPosition = new(-181.89f + (extraWidth * 0.5f), groupCreditsBackground.localPosition.y, groupCreditsBackground.localPosition.z);
                        groupCreditsBackground.sizeDelta = new(75.175f + extraWidth, groupCreditsBackground.sizeDelta.y);
                    }
                }
            }
        }
    }
}
