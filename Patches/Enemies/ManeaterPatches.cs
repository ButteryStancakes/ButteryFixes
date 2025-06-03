using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Netcode;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(CaveDwellerAI))]
    internal class ManeaterPatches
    {
        [HarmonyPatch(nameof(CaveDwellerAI.DoBabyAIInterval))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> CaveDwellerAI_Trans_DoBabyAIInterval(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo observingScrap = AccessTools.Field(typeof(CaveDwellerAI), nameof(CaveDwellerAI.observingScrap));
            MethodInfo despawn = AccessTools.Method(typeof(NetworkObject), nameof(NetworkObject.Despawn));
            for (int i = 4; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt && (MethodInfo)codes[i].operand == despawn && codes[i - 3].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 3].operand == observingScrap)
                {
                    codes.InsertRange(i - 4, [
                        new(OpCodes.Ldarg_0),
                        new(OpCodes.Ldfld, observingScrap),
                        new(OpCodes.Call, AccessTools.Method(typeof(NonPatchFunctions), nameof(NonPatchFunctions.BabyEatsScrap)))
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
