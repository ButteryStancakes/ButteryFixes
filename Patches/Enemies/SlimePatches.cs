using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    internal class SlimePatches
    {
        [HarmonyPatch(typeof(BlobAI), nameof(BlobAI.OnCollideWithPlayer))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> BlobAITransOnCollideWithPlayer(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo angeredTimer = AccessTools.Field(typeof(BlobAI), "angeredTimer");
            for (int i = 2; i < codes.Count; i++)
            {
                // fix erroneous < 0 check with <= 0
                if (codes[i].opcode == OpCodes.Bge_Un && codes[i - 2].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 2].operand == angeredTimer)
                {
                    codes[i].opcode = OpCodes.Bgt_Un;
                    Plugin.Logger.LogDebug("Transpiler (Hygrodere collision): Taming now possible without angering");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Hygrodere collision transpiler failed");
            return instructions;
        }
    }
}
