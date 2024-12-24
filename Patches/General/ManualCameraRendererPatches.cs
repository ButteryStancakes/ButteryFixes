using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch]
    class ManualCameraRendererPatches
    {
        [HarmonyPatch(typeof(ManualCameraRenderer), "Update")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ManualCameraRendererTransUpdate(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo y = AccessTools.Field(typeof(Vector3), nameof(Vector3.y)), z = AccessTools.Field(typeof(Vector3), nameof(Vector3.z));
            FieldInfo radarTargets = AccessTools.Field(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.radarTargets));
            for (int i = 4; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Brtrue && codes[i - 2].opcode == OpCodes.Ldc_R4 && (float)codes[i - 2].operand == -80f && codes[i - 3].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 3].operand == y)
                {
                    for (int j = i - 4; j > 0; j--)
                    {
                        if (codes[j].opcode == OpCodes.Ldfld && (FieldInfo)codes[j].operand == radarTargets)
                        {
                            List<CodeInstruction> clone = [];
                            for (int k = j - 1; k < i - 3; k++)
                                clone.Add(codes[k]);
                            clone.AddRange([
                                new(OpCodes.Ldfld, z),
                                new(OpCodes.Ldc_R4, 995f), // 400
                                new(OpCodes.Clt),
                                new(OpCodes.Brfalse, codes[i].operand),
                            ]);
                            codes.InsertRange(i + 1, clone);
                            Plugin.Logger.LogDebug("Transpiler (Radar): Hide compass icon when players are out of bounds");
                            return codes;
                        }
                    }
                }
            }

            Plugin.Logger.LogError("Radar transpiler failed");
            return instructions;
        }
    }
}
