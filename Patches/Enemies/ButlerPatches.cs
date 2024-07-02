using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    internal class ButlerPatches
    {
        [HarmonyPatch(typeof(ButlerEnemyAI), nameof(ButlerEnemyAI.OnCollideWithPlayer))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ButlerEnemyAITransOnCollideWithPlayer(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo timeSinceStealthStab = AccessTools.Field(typeof(ButlerEnemyAI), "timeSinceStealthStab");
            int startAt = -1, endAt = -1;
            for (int i = 0; i < codes.Count; i++)
            {
                if (startAt >= 0 && endAt >= 0)
                {
                    for (int j = startAt; j <= endAt; j++)
                    {
                        codes[j].opcode = OpCodes.Nop;
                        //codes[j].operand = null;
                    }
                    Plugin.Logger.LogDebug("Transpiler: Remove timestamp check (replace with prefix)");
                    return codes;
                }
                else if (startAt == -1)
                {
                    if (codes[i].opcode == OpCodes.Stfld && (FieldInfo)codes[i].operand == timeSinceStealthStab)
                        startAt = i - 2;
                }
                else if (i > startAt && codes[i].opcode == OpCodes.Ret)
                    endAt = i;
            }

            Plugin.Logger.LogError("Butler collision transpiler failed");
            return codes;
        }

        [HarmonyPatch(typeof(ButlerEnemyAI), nameof(ButlerEnemyAI.OnCollideWithPlayer))]
        [HarmonyPrefix]
        static bool ButlerEnemyAIPreOnCollideWithPlayer(ButlerEnemyAI __instance, ref float ___timeSinceStealthStab)
        {
            // recreate the timestamp check since we need to run some additional logic
            if (!__instance.isEnemyDead && __instance.currentBehaviourStateIndex != 2)
            {
                if (Time.realtimeSinceStartup - ___timeSinceStealthStab < 10f)
                    return false;

                Plugin.Logger.LogInfo("Butler rolls chance for \"stealth stab\"");
                if (Random.Range(0, 100) < 86)
                {
                    ___timeSinceStealthStab = Time.realtimeSinceStartup;
                    Plugin.Logger.LogInfo("Stealth stab chance failed (won't check for 10s)");
                    return false;
                }
                Plugin.Logger.LogInfo("Stealth stab chance succeeds (aggro for 3s)");
            }

            return true;
        }
    }
}
