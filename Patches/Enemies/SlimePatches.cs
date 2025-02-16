using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(BlobAI))]
    internal class SlimePatches
    {
        [HarmonyPatch(nameof(BlobAI.OnCollideWithPlayer))]
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

        // thanks Zaggy1024!
        [HarmonyPrefix]
        [HarmonyPatch(nameof(BlobAI.FixedUpdate))]
        private static bool FixedUpdatePrefix(BlobAI __instance)
        {
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BlobAI.Update))]
        private static void UpdatePostfix(BlobAI __instance)
        {
            if (!__instance.ventAnimationFinished)
                return;
            for (int i = 0; i < __instance.SlimeBonePositions.Length; i++)
            {
                if (Vector3.Distance(__instance.centerPoint.position, __instance.SlimeBonePositions[i]) > __instance.distanceOfRaysLastFrame[i])
                    __instance.SlimeBones[i].transform.position = Vector3.Lerp(__instance.SlimeBones[i].transform.position, __instance.SlimeBonePositions[i], 10f * Time.deltaTime);
                else
                    __instance.SlimeBones[i].transform.position = Vector3.Lerp(__instance.SlimeBones[i].transform.position, __instance.SlimeBonePositions[i], 5f * Time.deltaTime);
            }
        }
    }
}
