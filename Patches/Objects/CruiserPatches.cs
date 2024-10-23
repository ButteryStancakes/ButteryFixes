using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class CruiserPatches
    {
        static float radioPingTimestamp;

        [HarmonyPatch(typeof(VehicleController), "DestroyCar")]
        [HarmonyPostfix]
        static void PostDestroyCar(VehicleController __instance)
        {
            __instance.hoodAudio.mute = true;
            __instance.healthMeter.GetComponentInChildren<Renderer>().forceRenderingOff = true;
            __instance.turboMeter.GetComponentInChildren<Renderer>().forceRenderingOff = true;
        }

        [HarmonyPatch(typeof(VehicleController), "SetCarEffects")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TransSetCarEffects(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            MethodInfo playAudibleNoise = AccessTools.Method(typeof(RoundManager), nameof(RoundManager.PlayAudibleNoise));
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt && (MethodInfo)codes[i].operand == playAudibleNoise)
                {
                    for (int j = i + 1; j < codes.Count; j++)
                    {
                        if (codes[j].opcode == OpCodes.Br)
                        {
                            codes.Insert(i + 1, new CodeInstruction(OpCodes.Br, codes[j].operand));
                            Plugin.Logger.LogDebug("Transpiler (Cruiser noise alert): Fix 2 audible noises at once");
                            return codes;
                        }
                    }
                }
            }

            Plugin.Logger.LogError("Cruiser noise alert transpiler failed");
            return instructions;
        }

        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.SetRadioValues))]
        [HarmonyPostfix]
        static void PostSetRadioValues(VehicleController __instance)
        {
            if (__instance.IsServer && __instance.radioAudio.isPlaying && Time.realtimeSinceStartup > radioPingTimestamp)
            {
                radioPingTimestamp = Time.realtimeSinceStartup + 2f;
                RoundManager.Instance.PlayAudibleNoise(__instance.radioAudio.transform.position, 16f, Mathf.Min((__instance.radioAudio.volume + __instance.radioInterference.volume) * 0.5f, 0.9f), 0, false, 2692);
            }
        }

        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.CollectItemsInTruck))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> TransCollectItemsInTruck(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            MethodInfo overlapSphere = AccessTools.Method(typeof(Physics), nameof(Physics.OverlapSphere), [typeof(Vector3), typeof(float), typeof(int), typeof(QueryTriggerInteraction)]);
            for (int i = 1; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && (MethodInfo)codes[i].operand == overlapSphere && codes[i - 1].opcode == OpCodes.Ldc_I4_1)
                {
                    codes[i - 1].opcode = OpCodes.Ldc_I4_2;
                    Plugin.Logger.LogDebug("Transpiler (Cruiser collect): Auto-collect trigger colliders, for Teeth");
                    return codes;
                }
            }

            // reduce to a warning, because HighQuotaFixes includes a different solution for this issue, and Cruiser Additions patches it the exact same way as I do
            Plugin.Logger.LogWarning("Cruiser collect transpiler failed");
            return instructions;
        }
    }
}
