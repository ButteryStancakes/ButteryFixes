using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    internal class ForestGiantPatches
    {
        [HarmonyPatch(typeof(ForestGiantAI), nameof(ForestGiantAI.AnimationEventA))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ForestGiantAITransAnimationEventA(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo localPlayerController = AccessTools.Field(typeof(GameNetworkManager), nameof(GameNetworkManager.localPlayerController));
            for (int i = 4; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Brfalse && codes[i - 2].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 2].operand == localPlayerController)
                {
                    codes.InsertRange(i + 1, [
                        new(codes[i - 4].opcode), // local variable
                        new(OpCodes.Ldfld, ReflectionCache.IS_IN_HANGAR_SHIP_ROOM),
                        new(OpCodes.Brtrue, codes[i].operand),
                    ]);
                    Plugin.Logger.LogDebug("Transpiler (Forest giant death): No longer crush players in the ship");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Forest giant death transpiler failed");
            return instructions;
        }

        [HarmonyPatch(typeof(ForestGiantAI), "LookForPlayers")]
        [HarmonyPostfix]
        static void PostLookForPlayers(ForestGiantAI __instance)
        {
            for (int i = 0; i < __instance.playerStealthMeters.Length; i++)
                __instance.playerStealthMeters[i] = Mathf.Clamp01(__instance.playerStealthMeters[i]);
        }
    }
}
