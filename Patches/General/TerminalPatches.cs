using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace ButteryFixes.Patches.General
{
    internal class TerminalPatches
    {
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
    }
}
