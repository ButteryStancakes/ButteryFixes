using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(LungProp))]
    internal class ApparatusPatches
    {
        [HarmonyPatch(nameof(LungProp.Start))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        static void LungProp_Post_Start(LungProp __instance)
        {
            ScanNodeProperties scanNodeProperties = __instance.GetComponentInChildren<ScanNodeProperties>();
            if (scanNodeProperties != null)
            {
                if (scanNodeProperties.headerText == "Apparatice")
                    scanNodeProperties.headerText = "Apparatus";
                if (Configuration.showApparatusValue.Value)
                {
                    scanNodeProperties.scrapValue = __instance.scrapValue;
                    scanNodeProperties.subText = $"Value: ${scanNodeProperties.scrapValue}";
                }
                Plugin.Logger.LogDebug("Scan node: Apparatus");
            }
        }

        [HarmonyPatch(nameof(LungProp.DisconnectFromMachinery), MethodType.Enumerator)]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> LungProp_Trans_DisconnectFromMachinery(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            MethodInfo spawnEnemyGameObject = AccessTools.Method(typeof(RoundManager), nameof(RoundManager.SpawnEnemyGameObject));
            for (int i = 2; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt && (MethodInfo)codes[i].operand == spawnEnemyGameObject)
                {
                    codes.Insert(i + 2, new(OpCodes.Call, AccessTools.Method(typeof(NonPatchFunctions), nameof(NonPatchFunctions.OldBirdSpawnsFromApparatus))));
                    Plugin.Logger.LogDebug("Transpiler (Radiation warning): Add Old Bird values after spawning");
                    //i++;
                    return codes;
                }
            }

            Plugin.Logger.LogError("Radiation warning transpiler failed");
            return instructions;
        }
    }
}
