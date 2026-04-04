using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(GiantKiwiAI))]
    static class SapsuckerPatches
    {
        [HarmonyPatch(nameof(GiantKiwiAI.SpawnNestEggs))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> GiantKiwiAI_Trans_SpawnNestEggs(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            MethodInfo randomRange = AccessTools.Method(typeof(Random), nameof(Random.Range), [typeof(int), typeof(int)]), randomNext = AccessTools.Method(typeof(System.Random), nameof(System.Random.Next), [typeof(int), typeof(int)]);
            for (int i = 2; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && codes[i].operand as MethodInfo == randomRange)
                {
                    codes[i].opcode = OpCodes.Callvirt;
                    codes[i].operand = randomNext;
                    codes.Insert(i - 2, new(OpCodes.Ldloc_0));
                    Plugin.Logger.LogDebug("Transpiler (Sapsucker eggs): Replace random with seeded random");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Sapsucker eggs transpiler failed");
            return instructions;
        }
    }
}