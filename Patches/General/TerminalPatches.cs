using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
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
            if (!Plugin.GENERAL_IMPROVEMENTS)
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

            // fix cruiser price shown as $400 after price buff
            __instance.buyableVehicles[0].creditsWorth = 370;
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
                        Plugin.Logger.LogDebug("Transpiler (Terminal): Fix \"n\" on terminal when viewing monitor");
                        return codes;
                    }
                }
            }

            Plugin.Logger.LogDebug("Terminal transpiler failed");
            return codes;
        }

        [HarmonyPatch(typeof(Terminal), "TextPostProcess")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        static void TerminalPreTextPostProcess(Terminal __instance, ref string modifiedDisplayText)
        {
            if (Plugin.configScanOnShip.Value && modifiedDisplayText.Contains("[scanForItems]"))
            {
                bool inOrbit = StartOfRound.Instance.inShipPhase;
                if (!inOrbit)
                {
                    HangarShipDoor hangarShipDoor = Object.FindObjectOfType<HangarShipDoor>();
                    if (hangarShipDoor != null && !hangarShipDoor.buttonsEnabled)
                        inOrbit = true;
                }

                if (inOrbit)
                {
                    int objects = 0;
                    int value = 0;
                    foreach (GrabbableObject grabbableObject in Object.FindObjectsOfType<GrabbableObject>())
                    {
                        if (grabbableObject.itemProperties.isScrap && grabbableObject is not RagdollGrabbableObject)
                        {
                            objects++;
                            value += grabbableObject.scrapValue;
                        }
                    }
                    modifiedDisplayText = modifiedDisplayText.Replace("[scanForItems]", $"There are {objects} objects inside the ship, totalling at an exact value of ${value}.");
                }
            }
        }
    }
}
