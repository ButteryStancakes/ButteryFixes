using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Netcode;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    internal class ManeaterPatches
    {
        [HarmonyPatch(typeof(CaveDwellerAI), "DoBabyAIInterval")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> TransDoBabyAIInterval(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo observingScrap = AccessTools.Field(typeof(CaveDwellerAI), nameof(CaveDwellerAI.observingScrap));
            MethodInfo despawn = AccessTools.Method(typeof(NetworkObject), nameof(NetworkObject.Despawn));
            for (int i = 4; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt && (MethodInfo)codes[i].operand == despawn && codes[i - 3].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 3].operand == observingScrap)
                {
                    codes.InsertRange(i - 4, [
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, observingScrap),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NonPatchFunctions), nameof(NonPatchFunctions.BabyEatsScrap)))
                    ]);
                    Plugin.Logger.LogDebug("Transpiler (Maneater): Add eaten scrap to uncollected value");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Maneater transpiler failed");
            return instructions;
        }
    }
}
