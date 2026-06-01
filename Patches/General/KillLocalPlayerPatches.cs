using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ButteryFixes.Patches
{
    [HarmonyPatch(typeof(KillLocalPlayer))]
    static class KillLocalPlayerPatches
    {
        [HarmonyPatch(nameof(KillLocalPlayer.KillPlayer))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> KillLocalPlayer_Trans_KillPlayer(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 6; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand as MethodInfo == ReflectionCache.DAMAGE_PLAYER && codes[i - 6].opcode == OpCodes.Ldc_I4_0)
                {
                    codes[i - 6].opcode = OpCodes.Ldfld;
                    codes[i - 6].operand = AccessTools.Field(typeof(KillLocalPlayer), nameof(KillLocalPlayer.causeOfDeath));
                    codes.Insert(i - 6, new(OpCodes.Ldarg_0));
                    Plugin.Logger.LogDebug("Transpiler (Kill trigger): Use correct cause of death");
                    //i++;
                    return codes;
                }
            }

            Plugin.Logger.LogError("Kill trigger transpiler failed");
            return instructions;
        }
    }
}
