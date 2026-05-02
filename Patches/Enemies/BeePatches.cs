using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(RedLocustBees))]
    static class BeePatches
    {
        [HarmonyPatch(nameof(RedLocustBees.SpawnHiveNearEnemy))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> RedLocustBees_Trans_SpawnHiveNearEnemy(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 1; i < codes.Count; i++)
            {
                // literally why does this work but not casting it as a byte
                if (codes[i].opcode == OpCodes.Ldloc_2 && codes[i - 1].opcode == OpCodes.Ldloc_S && codes[i - 1].operand.ToString().EndsWith("(4)"))
                {
                    codes.InsertRange(i, [
                        new(OpCodes.Ldloc_2),
                        new(OpCodes.Call, AccessTools.Method(typeof(NonPatchFunctions), nameof(NonPatchFunctions.RerollHivePrice))),
                    ]);
                    Plugin.Logger.LogDebug("Transpiler (Bee spawn): Hook custom price function");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Bee spawn transpiler failed");
            return instructions;
        }

        [HarmonyPatch(nameof(RedLocustBees.SpawnHiveClientRpc))]
        [HarmonyPostfix]
        static void RedLocustBees_Post_SpawnHiveClientRpc(RedLocustBees __instance)
        {
            if (__instance.hasSpawnedHive)
                ScrapTracker.Track(__instance.hive);
        }
    }
}
