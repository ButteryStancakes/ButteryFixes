using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(StingrayAI))]
    static class GunkfishPatches
    {
        [HarmonyPatch(nameof(StingrayAI.HitEnemy))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> FlowermanAI_Trans_HitEnemy(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo enemyHP = AccessTools.Field(typeof(EnemyAI), nameof(EnemyAI.enemyHP));
            for (int i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld && (FieldInfo)codes[i].operand == enemyHP && codes[i + 1].opcode == OpCodes.Conv_R4 && codes[i + 2].opcode == OpCodes.Ldc_R4 && (float)codes[i + 2].operand == 0f && codes[i + 3].opcode == OpCodes.Bge_Un)
                {
                    codes[i + 1].opcode = OpCodes.Nop;
                    codes[i + 2].opcode = OpCodes.Ldc_I4_0;
                    codes[i + 3].opcode = OpCodes.Bgt;
                    Plugin.Logger.LogDebug("Transpiler (Gunkfish damage): Die at 0 HP");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Gunkfish damage transpiler failed");
            return instructions;
        }
    }
}
