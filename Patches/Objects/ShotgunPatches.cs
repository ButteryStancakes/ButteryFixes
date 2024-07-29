using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class ShotgunPatches
    {
        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ReloadGunEffectsClientRpc))]
        [HarmonyPostfix]
        static void PostReloadGunEffectsClientRpc(ShotgunItem __instance, bool start)
        {
            // controls shells appearing/disappearing during reload for all clients (except for the one holding the gun)
            if (start && !__instance.IsOwner)
            {
                __instance.shotgunShellLeft.enabled = __instance.shellsLoaded > 0;
                __instance.shotgunShellRight.enabled = false;
                __instance.StartCoroutine(NonPatchFunctions.ShellsAppearAfterDelay(__instance));
                Plugin.Logger.LogInfo("Shotgun was reloaded by another client; animating shells");
            }
        }

        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.Update))]
        [HarmonyPostfix]
        static void ShotgunItemPostUpdate(ShotgunItem __instance)
        {
            // shells should render during the reload animation (this specific patch only works for players)
            if (__instance.isReloading)
            {
                __instance.shotgunShellLeft.forceRenderingOff = false;
                __instance.shotgunShellRight.forceRenderingOff = false;
            }
        }

        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.Start))]
        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.DiscardItem))]
        [HarmonyPostfix]
        static void DontRenderShotgunShells(ShotgunItem __instance)
        {
            __instance.shotgunShellLeft.forceRenderingOff = true;
            __instance.shotgunShellRight.forceRenderingOff = true;
        }

        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ShootGun))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ShotgunItemTransShootGun(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = instructions.ToList();

            bool fixEarsRinging = false;
            for (int i = 2; i < codes.Count; i++)
            {
                // first distance check for tinnitus/screenshake
                if (!fixEarsRinging && codes[i].opcode == OpCodes.Bge_Un && codes[i - 2].opcode == OpCodes.Ldloc_2)
                {
                    for (int j = i + 1; j < codes.Count - 1; j++)
                    {
                        int insertAt = -1;
                        if (codes[j + 1].opcode == OpCodes.Ldloc_2)
                        {
                            // first jump from if/else branches
                            if (insertAt >= 0 && codes[j].opcode == OpCodes.Br)
                            {
                                codes.Insert(insertAt, new CodeInstruction(OpCodes.Br, codes[j].operand));
                                Plugin.Logger.LogDebug("Transpiler (Shotgun blast): Fix ear-ringing severity in extremely close range");
                                fixEarsRinging = true;
                                break;
                            }
                            // the end of the first if branch
                            else if (insertAt < 0 && codes[j].opcode == OpCodes.Stloc_S)
                                insertAt = j + 1;
                        }
                    }
                }
                else if (codes[i].opcode == OpCodes.Newarr && (System.Type)codes[i].operand == typeof(RaycastHit) && codes[i - 1].opcode == OpCodes.Ldc_I4_S && (sbyte)codes[i - 1].operand == 10)
                {
                    codes[i - 1].operand = 50;
                    Plugin.Logger.LogDebug("Transpiler (Shotgun blast): Resize target colliders array");
                }
                else if (codes[i].opcode == OpCodes.Call && codes[i].operand.ToString().Contains("SphereCastNonAlloc"))
                {
                    codes.InsertRange(i + 2, [
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldloca_S, codes[i + 1].operand),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldflda, ReflectionCache.ENEMY_COLLIDERS),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NonPatchFunctions), nameof(NonPatchFunctions.ShotgunPreProcess))),
                    ]);
                    Plugin.Logger.LogDebug("Transpiler (Shotgun blast): Pre-process shotgun targets");
                }
            }

            return codes;
        }
    }
}
