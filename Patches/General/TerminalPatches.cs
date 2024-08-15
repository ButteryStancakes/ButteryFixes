using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch]
    internal class TerminalPatches
    {
        [HarmonyPatch(typeof(Terminal), "Start")]
        [HarmonyPostfix]
        static void TerminalPostStart(Terminal __instance)
        {
            if (!Compatibility.INSTALLED_GENERAL_IMPROVEMENTS)
                __instance.SetItemSales(); // seems like this is necessary because InitializeItemSalesPercentages gets called after StartOfRound.Start

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
                        Plugin.Logger.LogInfo("Price: Cruiser");
                    }
                }

                AudioSource clipboardCruiser = cruiser.secondaryPrefab?.transform.GetComponent<AudioSource>();
                if (clipboardCruiser != null)
                {
                    clipboardCruiser.rolloffMode = AudioRolloffMode.Linear;
                    Plugin.Logger.LogInfo("Audio rolloff: Clipboard (Cruiser)");
                }
            }

            if (buy != null)
            {
                TerminalNode buyWelcomeMat = buy.compatibleNouns.FirstOrDefault(noun => noun.noun.name == "WelcomeMat")?.result;
                if (buyWelcomeMat != null)
                {
                    buyWelcomeMat.itemCost = 40;
                    Plugin.Logger.LogInfo("Price: Welcome mat");
                }
            }
        }

        [HarmonyPatch(typeof(Terminal), "TextPostProcess")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> TransTextPostProcess(IEnumerable<CodeInstruction> instructions)
        {
            if (Compatibility.INSTALLED_GENERAL_IMPROVEMENTS)
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
                        Plugin.Logger.LogDebug("Transpiler (Terminal text): Fix \"n\" on terminal when viewing monitor");
                        return codes;
                    }
                }
            }

            Plugin.Logger.LogError("Terminal text transpiler failed");
            return codes;
        }

        [HarmonyPatch(typeof(Terminal), "TextPostProcess")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        static void TerminalPreTextPostProcess(Terminal __instance, ref string modifiedDisplayText)
        {
            if (Configuration.scanOnShip.Value && modifiedDisplayText.Contains("[scanForItems]"))
            {
                bool inOrbit = StartOfRound.Instance.inShipPhase;
                if (!inOrbit)
                {
                    HangarShipDoor hangarShipDoor = Object.FindObjectOfType<HangarShipDoor>();
                    if (hangarShipDoor != null && !hangarShipDoor.buttonsEnabled)
                        inOrbit = true;
                }

                string vehicleText = string.Empty;
                if (inOrbit || StartOfRound.Instance.currentLevel.name == "CompanyBuildingLevel")
                {
                    int objects = 0;
                    int value = 0;
                    Transform cruiser = Object.FindObjectOfType<VehicleController>()?.transform;
                    foreach (GrabbableObject grabbableObject in Object.FindObjectsOfType<GrabbableObject>())
                    {
                        bool inVehicle = StartOfRound.Instance.isObjectAttachedToMagnet && cruiser != null && grabbableObject.transform.parent == cruiser;
                        if ((grabbableObject.isInShipRoom || grabbableObject.isInElevator || inVehicle) && grabbableObject.itemProperties.isScrap && grabbableObject is not RagdollGrabbableObject)
                        {
                            objects++;
                            value += grabbableObject.scrapValue;
                            if (inVehicle && string.IsNullOrEmpty(vehicleText))
                                vehicleText = " and Cruiser";
                        }
                    }
                    modifiedDisplayText = modifiedDisplayText.Replace("[scanForItems]", $"There are {objects} objects inside the ship{vehicleText}, totalling at an exact value of ${value}.");
                }
            }

            if (modifiedDisplayText.Contains("[numberOfItemsOnRoute]") && __instance.vehicleInDropship)
                modifiedDisplayText = modifiedDisplayText.Replace("[numberOfItemsOnRoute]", "1 purchased vehicle on route.");
        }

        [HarmonyPatch(typeof(Terminal), "ParsePlayerSentence")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> TransParsePlayerSentence(IEnumerable<CodeInstruction> instructions)
        {
            if (Compatibility.INSTALLED_GENERAL_IMPROVEMENTS)
                return instructions;

            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo playerDefinedAmount = AccessTools.Field(typeof(Terminal), nameof(Terminal.playerDefinedAmount));
            MethodInfo clamp = AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), [typeof(int), typeof(int), typeof(int)]);
            for (int i = 2; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Stfld && (FieldInfo)codes[i].operand == playerDefinedAmount && codes[i - 1].opcode == OpCodes.Call && (MethodInfo)codes[i - 1].operand == clamp && codes[i - 2].opcode == OpCodes.Ldc_I4_S)
                {
                    codes[i - 2].operand = 12;
                    Plugin.Logger.LogDebug("Transpiler (Order capacity): Allow bulk purchases of 12");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Order capacity transpiler failed");
            return codes;
        }

        [HarmonyPatch(typeof(Terminal), "LoadNewNodeIfAffordable")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> TransLoadNewNodeIfAffordable(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            //FieldInfo buyItemIndex = AccessTools.Field(typeof(TerminalNode), nameof(TerminalNode.buyItemIndex));
            //FieldInfo numberOfItemsInDropship = AccessTools.Field(typeof(Terminal), nameof(Terminal.numberOfItemsInDropship));
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
            return codes;
        }
    }
}
