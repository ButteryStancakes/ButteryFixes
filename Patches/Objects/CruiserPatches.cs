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
            return codes;
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
    }
}
