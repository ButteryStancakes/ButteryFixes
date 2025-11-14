using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.AI;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(ManualCameraRenderer))]
    class ManualCameraRendererPatches
    {
        static Vector3[] points = new Vector3[20];

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
                // lose signal of dead bodies
                if (Configuration.noBodyNoSignal.Value && !__instance.enableHeadMountedCam && __instance.targetedPlayer != null && __instance.targetedPlayer.isPlayerDead && __instance.targetedPlayer.deadBody == null && __instance.targetedPlayer.redirectToEnemy == null)
                {
                    __instance.LostSignalUI.SetActive(true);
                    return;
                }

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

        /*
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
        */

        [HarmonyPatch(nameof(ManualCameraRenderer.SetLineToExitFromRadarTarget))]
        [HarmonyPrefix]
        static bool ManualCameraRenderer_Pre_SetLineToExitFromRadarTarget(ManualCameraRenderer __instance)
        {
            // radar is not active
            if (__instance.overrideCameraForOtherUse || !__instance.screenEnabledOnLocalClient || __instance.playerIsInCaves || __instance.LostSignalUI.activeSelf)
            {
                __instance.lineFromRadarTargetToExit.enabled = false;
                return false;
            }

            // radar is not targeting a valid player
            if (__instance.targetedPlayer == null || (!__instance.targetedPlayer.isPlayerControlled && !__instance.targetedPlayer.isPlayerDead))
            {
                __instance.lineFromRadarTargetToExit.enabled = false;
                return false;
            }

            // targeted player is not inside the building
            if ((!__instance.targetedPlayer.isInsideFactory && !__instance.targetedPlayer.isPlayerDead) || __instance.mapCamera.transform.position.y >= -80f || (__instance.targetedPlayer.isPlayerDead && __instance.targetedPlayer.redirectToEnemy != null && __instance.targetedPlayer.redirectToEnemy.isOutside))
            {
                __instance.lineFromRadarTargetToExit.enabled = false;
                return false;
            }

            // player's body is missing
            if ((__instance.targetedPlayer.isPlayerDead || !__instance.targetedPlayer.isPlayerControlled) && __instance.targetedPlayer.deadBody == null && __instance.targetedPlayer.redirectToEnemy == null)
            {
                __instance.lineFromRadarTargetToExit.enabled = false;
                return false;
            }

            __instance.lineFromRadarTargetToExit.enabled = true;

            if (__instance.updateLineInterval > 0f)
            {
                __instance.updateLineInterval -= Time.deltaTime;

                __instance.dottedLineOffset -= Time.deltaTime;
                Material dottedLineMat = __instance.lineFromRadarTargetToExit.material;
                Vector2 offset = new(__instance.dottedLineOffset, 0f);
                dottedLineMat.SetTextureOffset("_UnlitColorMap", offset); // proper texture map
                dottedLineMat.SetTextureOffset("_MainTex", offset); // in case of fallback shader

                // if path has <50 corners, vanilla makes line start from the target, but this can also cause ugly contortions
                /*if (__instance.setLineIntervalTo < 1f)
                    __instance.lineFromRadarTargetToExit.SetPosition(0, __instance.mapCamera.transform.position + (2.5f * Vector3.down));*/
            }
            else
            {
                Vector3 lineTarget = NonPatchFunctions.GetTrueExitPoint();

                // exit path can't be calculated
                if (lineTarget == Vector3.zero || !NavMesh.CalculatePath(__instance.mapCamera.transform.position + (3.75f * Vector3.down), lineTarget, NavMesh.AllAreas, __instance.path1))
                    return false;

                // dynamic refresh rate, based on path complexity
                if (__instance.path1.corners.Length > 50)
                    __instance.setLineIntervalTo = 2f;
                else if (__instance.path1.corners.Length < 36)
                    __instance.setLineIntervalTo = 0.4f;

                // update path vertices
                __instance.lineFromRadarTargetToExit.positionCount = Mathf.Clamp(__instance.path1.corners.Length, 1, 20);
                points[0] = __instance.mapCamera.transform.position + (2.5f * Vector3.down);
                for (int i = 1; i < __instance.lineFromRadarTargetToExit.positionCount; i++)
                    points[i] = __instance.path1.corners[i] + (1.25f * Vector3.up);
                __instance.lineFromRadarTargetToExit.SetPositions(points);

                // cooldown
                __instance.updateLineInterval = __instance.setLineIntervalTo;
            }

            return false;
        }
    }
}
