using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(ForestGiantAI))]
    static class ForestGiantPatches
    {
        [HarmonyPatch(nameof(ForestGiantAI.AnimationEventA))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ForestGiantAI_Trans_AnimationEventA(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            bool shipCheck = false, causeOfDeath = false;
            for (int i = 6; i < codes.Count - 1; i++)
            {
                if (!shipCheck && codes[i].opcode == OpCodes.Brfalse && codes[i - 2].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 2].operand == ReflectionCache.LOCAL_PLAYER_CONTROLLER)
                {
                    codes.InsertRange(i + 1, [
                        new(codes[i - 4].opcode), // local variable
                        new(OpCodes.Ldfld, ReflectionCache.IS_IN_HANGAR_SHIP_ROOM),
                        new(OpCodes.Brtrue, codes[i].operand),
                    ]);
                    i += 3;
                    Plugin.Logger.LogDebug("Transpiler (Forest giant death): No longer crush players in the ship");
                    shipCheck = true;
                }
                else if (!causeOfDeath && codes[i].opcode == OpCodes.Callvirt && codes[i].operand as MethodInfo == ReflectionCache.KILL_PLAYER && codes[i - 6].opcode == OpCodes.Ldc_I4_2)
                {
                    codes[i - 6].opcode = OpCodes.Ldc_I4_S;
                    codes[i - 6].operand = (sbyte)CauseOfDeath.Crushing;
                    Plugin.Logger.LogDebug("Transpiler (Forest giant death): Replace \"Gravity\" with \"Crushing\"");
                    causeOfDeath = true;
                }

                if (shipCheck && causeOfDeath)
                    return codes;
            }

            Plugin.Logger.LogError("Forest giant death transpiler failed");
            return instructions;
        }

        [HarmonyPatch(typeof(ForestGiantAI), nameof(ForestGiantAI.GiantSeePlayerEffect))]
        [HarmonyPrefix]
        static bool ForestGiantAI_Pre_GiantSeePlayerEffect(EnemyAI __instance)
        {
            if (__instance.eye == null || GameNetworkManager.Instance.localPlayerController.gameplayCamera == null)
                return true;

            float dist = Vector3.Distance(__instance.eye.position, GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position);

            if (dist > 16f && GameNetworkManager.Instance.localPlayerController.isUnderwater)
                return false;

            if (dist > 30f && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
                return false;

            float suspicionMult = 1f;
            if (!GameNetworkManager.Instance.localPlayerController.isCrouching)
                suspicionMult += 1f;
            if (GameNetworkManager.Instance.localPlayerController.timeSincePlayerMoving < 0.1f)
                suspicionMult += 1f;

            if (suspicionMult < 2f && dist >= 45f)
                return false;

            return true;
        }
    }
}
