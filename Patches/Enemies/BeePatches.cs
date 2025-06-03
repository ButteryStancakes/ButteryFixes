using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(RedLocustBees))]
    internal class BeePatches
    {
        [HarmonyPatch(nameof(RedLocustBees.SpawnHiveNearEnemy))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> RedLocustBees_Trans_SpawnHiveNearEnemy(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo localPlayerController = AccessTools.Field(typeof(GameNetworkManager), nameof(GameNetworkManager.localPlayerController));
            for (int i = 1; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldloc_1 && codes[i - 1].opcode == OpCodes.Ldloc_3)
                {
                    codes.InsertRange(i, [
                        new(OpCodes.Ldloc_1),
                        new(OpCodes.Call, AccessTools.Method(typeof(NonPatchFunctions), nameof(NonPatchFunctions.RerollHivePrice))),
                    ]);
                    Plugin.Logger.LogDebug("Transpiler (Bee spawn): Hook custom price function");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Bee spawn transpiler failed");
            return instructions;
        }
    }
}
