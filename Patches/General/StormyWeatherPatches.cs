using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch]
    class StormyWeatherPatches
    {
        [HarmonyPatch(typeof(StormyWeather), "Update")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> StormyWeatherTransUpdate(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo metalObjects = AccessTools.Field(typeof(StormyWeather), "metalObjects");
            FieldInfo setStaticGrabbableObject = AccessTools.Field(typeof(StormyWeather), "setStaticGrabbableObject");
            FieldInfo targetingMetalObject = AccessTools.Field(typeof(StormyWeather), "targetingMetalObject");
            FieldInfo isInFactory = AccessTools.Field(typeof(GrabbableObject), nameof(GrabbableObject.isInFactory));
            int insertAt = -1;
            for (int i = 6; i < codes.Count; i++)
            {
                if (insertAt < 0 && codes[i].opcode == OpCodes.Brtrue && codes[i - 1].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 1].operand == isInFactory && codes[i - 4].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 4].operand == metalObjects && codes[i - 3].opcode.ToString().StartsWith("ldloc") && codes[i - 2].ToString().Contains("get_Item"))
                    insertAt = i - 5;
                else if (codes[i].opcode == OpCodes.Ldfld && (FieldInfo)codes[i].operand == setStaticGrabbableObject)
                {
                    if (codes[i - 6].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 6].operand == metalObjects && codes[i - 5].opcode.ToString().StartsWith("ldloc") && codes[i - 4].ToString().Contains("get_Item"))
                    {
                        codes[i].operand = metalObjects;
                        codes.InsertRange(i + 1, [
                            new CodeInstruction(codes[i - 5].opcode, codes[i - 5].operand), // should be ldloc.2
                            new CodeInstruction(codes[i - 4].opcode, codes[i - 4].operand) // should be callvirt get_Item
                        ]);
                        Plugin.Logger.LogDebug("Transpiler (Stormy weather): Replace \"setStaticGrabbableObject\" with \"metalObjects[i]\"");
                        i += 2;
                    }
                    else if (codes[i - 4].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 4].operand == targetingMetalObject)
                    {
                        codes[i].operand = targetingMetalObject;
                        Plugin.Logger.LogDebug("Transpiler (Stormy weather): Replace \"setStaticGrabbableObject\" with \"targetingMetalObject\"");
                    }
                }
            }
            if (insertAt >= 0)
            {
                codes.InsertRange(insertAt, [
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, metalObjects),
                    new CodeInstruction(codes[insertAt + 2].opcode, codes[insertAt + 2].operand), // should be ldloc.2
                    new CodeInstruction(codes[insertAt + 3].opcode, codes[insertAt + 3].operand), // should be callvirt get_Item
                    new CodeInstruction(OpCodes.Ldnull),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Object), "op_Inequality", [typeof(Object), typeof(Object)])),
                    new CodeInstruction(OpCodes.Brfalse, codes[insertAt + 5].operand)
                ]);
                Plugin.Logger.LogDebug("Transpiler (Stormy weather): Null check when iterating metalObjects");
            }
            else
                Plugin.Logger.LogWarning("Transpiler (Stormy weather): Unable to patch with null check. Is another mod transpiling \"StormyWeather.Update()\"?");

            return codes;
        }
    }
}
