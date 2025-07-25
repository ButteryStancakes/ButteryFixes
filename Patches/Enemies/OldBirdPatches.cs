﻿using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(RadMechAI))]
    internal class OldBirdPatches
    {
        [HarmonyPatch(nameof(RadMechAI.Stomp))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> RadMechAI_Trans_Stomp(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].opcode == OpCodes.Ldloc_1 && codes[i + 1].opcode == OpCodes.Ldarg_S && codes[i + 2].opcode == OpCodes.Bge_Un)
                {
                    Label label = generator.DefineLabel();
                    for (int j = i + 3; j < codes.Count; j++)
                    {
                        if (codes[j].opcode == OpCodes.Dup)
                        {
                            codes[j - 1].labels.Add(label);
                            codes.InsertRange(i + 3,
                            [
                                new(OpCodes.Ldloc_0),
                                new(OpCodes.Ldfld, ReflectionCache.IS_IN_HANGAR_SHIP_ROOM),
                                new(OpCodes.Brtrue, label)
                            ]);
                            Plugin.Logger.LogDebug("Transpiler (Old Bird stomp): Don't damage players in ship");
                            return codes;
                        }
                    }
                }
            }

            Plugin.Logger.LogError("Old Bird stomp transpiler failed");
            return instructions;
        }

        [HarmonyPatch(nameof(RadMechAI.DoAIInterval))]
        [HarmonyPostfix]
        static void RadMechAI_Post_DoAIInterval(RadMechAI __instance)
        {
            // avoid sliding in game over cutscene
            if (__instance.IsOwner && StartOfRound.Instance.allPlayersDead)
                __instance.agent.speed = 0f;
        }
    }
}
