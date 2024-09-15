using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    internal class BrackenPatches
    {
        [HarmonyPatch(typeof(FlowermanAI), nameof(FlowermanAI.HitEnemy))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> FlowermanAITransHitEnemy(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo angerMeter = AccessTools.Field(typeof(FlowermanAI), "angerMeter");
            for (int i = 2; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Stfld && (FieldInfo)codes[i].operand == angerMeter)
                {
                    for (int j = i - 2; j < codes.Count; j++)
                    {
                        if (codes[j].opcode == OpCodes.Ret)
                        {
                            Plugin.Logger.LogDebug("Transpiler (Bracken damage): Remove aggro on hit (replace with postfix)");
                            return codes;
                        }
                        codes[j].opcode = OpCodes.Nop;
                    }
                }
            }

            Plugin.Logger.LogError("Bracken damage transpiler failed");
            return instructions;
        }

        [HarmonyPatch(typeof(FlowermanAI), nameof(FlowermanAI.HitEnemy))]
        [HarmonyPostfix]
        static void FlowermanAIPostHitEnemy(FlowermanAI __instance, PlayerControllerB playerWhoHit)
        {
            if (playerWhoHit != null)
            {
                __instance.angerMeter = 11f;
                __instance.angerCheckInterval = 1f;
            }
            else
                Plugin.Logger.LogDebug("Bracken was damaged by an enemy; don't max aggro");
            __instance.AddToAngerMeter(0.1f);
        }
    }
}
