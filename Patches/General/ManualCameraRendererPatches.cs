using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(ManualCameraRenderer))]
    class ManualCameraRendererPatches
    {
        [HarmonyPatch(nameof(ManualCameraRenderer.Update))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ManualCameraRenderer_Trans_Update(IEnumerable<CodeInstruction> instructions)
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
                            Plugin.Logger.LogDebug("Transpiler (Radar): Hide arrow when players are out of bounds");
                            return codes;
                        }
                    }
                }
            }

            Plugin.Logger.LogError("Radar transpiler failed");
            return instructions;
        }

        [HarmonyPatch(nameof(ManualCameraRenderer.LateUpdate))]
        [HarmonyPostfix]
        static void ManualCameraRenderer_Post_LateUpdate(ManualCameraRenderer __instance)
        {
            // fix screen toggling on a delay (unlike the head mounted cams)
            if (__instance.LostSignalUI != null)
            {
                if (__instance.playerIsInCaves && (StartOfRound.Instance.inShipPhase || __instance.overrideCameraForOtherUse || RoundManager.Instance.currentDungeonType != 4 || __instance.targetedPlayer == null || !__instance.targetedPlayer.isInsideFactory))
                {
                    __instance.playerIsInCaves = false;
                    if (__instance.checkingCaveNode >= 0)
                    {
                        __instance.checkCaveInterval = 1f;
                        __instance.checkingCaveNode = -1;
                    }
                }

                __instance.LostSignalUI.SetActive(__instance.playerIsInCaves);
            }
        }

        // vanilla logic is too susceptible to problems... needs to be replaced
        [HarmonyPatch(nameof(ManualCameraRenderer.CheckIfPlayerIsInCaves))]
        [HarmonyPrefix]
        static bool ManualCameraRenderer_Pre_CheckIfPlayerIsInCaves(ManualCameraRenderer __instance, Vector3 targetPosition)
        {
            if (Compatibility.DISABLE_SIGNAL_PATCH)
                return true;

            // fallback, in case my system is broken
            if (RoundManager.Instance.currentDungeonType == 4 && GlobalReferences.caveTiles.Count < 1)
                return true;

            if (!__instance.screenEnabledOnLocalClient && !__instance.overrideRadarCameraOnAlways)
                return false;

            if (RoundManager.Instance.currentDungeonType != 4 || __instance.targetedPlayer == null || !__instance.targetedPlayer.isInsideFactory || GlobalReferences.caveTiles.Count < 1)
            {
                __instance.playerIsInCaves = false;
                __instance.checkCaveInterval = 1f;
                __instance.checkingCaveNode = -1;
                return false;
            }

            if (__instance.checkingCaveNode == -1)
            {
                if (__instance.checkCaveInterval <= 0f)
                {
                    __instance.checkingCaveNode = 0;
                    __instance.checkCaveInterval = 1f;
                }
                else
                    __instance.checkCaveInterval -= Time.deltaTime;
            }
            else
            {
                if (__instance.checkingCaveNode >= GlobalReferences.caveTiles.Count)
                {
                    __instance.playerIsInCaves = false;
                    __instance.checkingCaveNode = -1;
                    //Plugin.Logger.LogDebug($"Player is not within bounds of any cave tile");
                    return false;
                }

                //Plugin.Logger.LogDebug($"Checking tile #{__instance.checkingCaveNode}");

                if (GlobalReferences.caveTiles[__instance.checkingCaveNode].Contains(targetPosition))
                {
                    //Plugin.Logger.LogDebug($"Player is in bounds of tile #{__instance.checkingCaveNode}");
                    __instance.playerIsInCaves = true;
                    __instance.checkingCaveNode = -1;
                    return false;
                }

                __instance.checkingCaveNode++;
            }

            return false;
        }

        [HarmonyPatch(nameof(ManualCameraRenderer.SetLineToExitFromRadarTarget))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ManualCameraRenderer_Trans_SetLineToExitFromRadarTarget(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            MethodInfo findMainEntrancePosition = AccessTools.Method(typeof(RoundManager), nameof(RoundManager.FindMainEntrancePosition));
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && codes[i].operand as MethodInfo == findMainEntrancePosition)
                {
                    codes[i].operand = AccessTools.Method(typeof(NonPatchFunctions), nameof(NonPatchFunctions.GetTrueExitPoint));
                    Plugin.Logger.LogDebug("Transpiler (Radar line): Optimize and support elevator");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Radar line transpiler failed");
            return instructions;
        }

        [HarmonyPatch(nameof(ManualCameraRenderer.SetLineToExitFromRadarTarget))]
        [HarmonyPrefix]
        static bool ManualCameraRenderer_Pre_SetLineToExitFromRadarTarget(ManualCameraRenderer __instance)
        {
            // fix teleported/missing bodies still showing radar lines
            if (__instance.overrideCameraForOtherUse || __instance.targetedPlayer == null || __instance.mapCamera.transform.position.y >= -80f || ((__instance.targetedPlayer.isPlayerDead || !__instance.targetedPlayer.isPlayerControlled) && __instance.targetedPlayer.deadBody == null && __instance.targetedPlayer.redirectToEnemy == null))
            {
                __instance.lineFromRadarTargetToExit.enabled = false;
                return false;
            }

            return true;
        }
    }
}
