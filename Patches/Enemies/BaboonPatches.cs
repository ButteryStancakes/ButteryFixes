using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(BaboonBirdAI))]
    static class BaboonPatches
    {
        [HarmonyPatch(nameof(BaboonBirdAI.DoAIInterval))]
        [HarmonyPrefix]
        static void BaboonBirdAI_Pre_DoAIInterval(BaboonBirdAI __instance)
        {
            // fixes potential nullref
            if (__instance.currentBehaviourStateIndex == 2 && __instance.focusingOnThreat && __instance.focusedThreat != null && __instance.focusedThreat.threatScript == null)
            {
                Plugin.Logger.LogWarning($"Baboon #{__instance.GetInstanceID()} is trying to focus on a threat (type \"{__instance.focusedThreat.type}\") that doesn't exist, this would've caused errors in vanilla");
                __instance.StopFocusingThreat();
            }
        }

        [HarmonyPatch(nameof(BaboonBirdAI.OnCollideWithPlayer))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> BaboonBirdAI_Trans_OnCollideWithPlayer(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 6; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand as MethodInfo == ReflectionCache.DAMAGE_PLAYER && codes[i - 6].opcode == OpCodes.Ldc_I4_0)
                {
                    codes[i - 6].opcode = OpCodes.Ldc_I4_S;
                    codes[i - 6].operand = (sbyte)CauseOfDeath.Stabbing;
                    Plugin.Logger.LogDebug("Transpiler (Baboon hawk): Replace \"Unknown\" with \"Stabbing\"");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Baboon hawk transpiler failed");
            return instructions;
        }
    }
}
