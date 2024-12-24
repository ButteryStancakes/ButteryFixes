using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    internal class OldBirdPatches
    {
        [HarmonyPatch(typeof(RadMechAI), "Stomp")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> RadMechAITransStomp(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
    }
}
