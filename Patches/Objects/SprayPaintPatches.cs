﻿using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    class SprayPaintPatches
    {
        [HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.LateUpdate))]
        [HarmonyPrefix]
        static void SprayPaintItemPreLateUpdate(SprayPaintItem __instance, ref bool ___isSpraying, ref float ___sprayInterval, DecalProjector ___delayedSprayPaintDecal)
        {
            if (___isSpraying)
            {
                if (__instance.itemProperties.canBeInspected && __instance.playerHeldBy != null && __instance.playerHeldBy.IsInspectingItem)
                {
                    ___isSpraying = false;
                    __instance.StopSpraying();
                }
                // make painting sequential decals smoother
                else if (!Compatibility.DISABLE_SPRAY_PAINT_PATCHES && !__instance.isWeedKillerSprayBottle && ___sprayInterval > 0f && ___delayedSprayPaintDecal != null && ___delayedSprayPaintDecal.enabled && ___sprayInterval < (__instance.sprayIntervalSpeed - Time.fixedDeltaTime))
                    ___sprayInterval = 0f;
            }
        }

        [HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.ItemActivate))]
        [HarmonyPrefix]
        static void SprayPaintItemPreItemActivate(SprayPaintItem __instance, ref bool buttonDown, float ___sprayCanTank)
        {
            if (buttonDown && __instance.itemProperties.canBeInspected && __instance.playerHeldBy != null && __instance.playerHeldBy.IsInspectingItem && ___sprayCanTank > 0f)
                buttonDown = false;
        }

        [HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.DiscardItem))]
        [HarmonyPrefix]
        static void SprayPaintItemPreDiscardItem(SprayPaintItem __instance)
        {
            // vanilla calls this after base.DiscardItem() which means this reference will always be null
            if (__instance.playerHeldBy != null)
            {
                __instance.playerHeldBy.activatingItem = false;
                __instance.playerHeldBy.equippedUsableItemQE = false;
            }
        }

        [HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.EquipItem))]
        [HarmonyPostfix]
        static void SprayPaintItemPostEquipItem(SprayPaintItem __instance)
        {
            // can't shake weed killer
            if (__instance.isWeedKillerSprayBottle)
                __instance.playerHeldBy.equippedUsableItemQE = false;
        }

        [HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.Start))]
        [HarmonyPostfix]
        static void SprayPaintItemPostStart(SprayPaintItem __instance)
        {
            if (!Compatibility.DISABLE_SPRAY_PAINT_PATCHES && !__instance.isWeedKillerSprayBottle)
            {
                __instance.sprayIntervalSpeed = 0.05f;
                // 600 * (0.08 / 0.05) * (0.175 / 0.1) = 1680, rounded to multiple of 200
                __instance.maxSprayPaintDecals = 1600;
            }
        }

        [HarmonyPatch(typeof(SprayPaintItem), "AddSprayPaintLocal")]
        [HarmonyPrefix]
        static void PreAddSprayPaintLocal(SprayPaintItem __instance, ref int ___sprayPaintMask, ref int ___addSprayPaintWithFrameDelay, DecalProjector ___delayedSprayPaintDecal)
        {
            if (Compatibility.DISABLE_SPRAY_PAINT_PATCHES)
                return;

            // don't collide with player (same bug as jetpack)
            int placeableShipObjects = (1 << 26);
            if (__instance.isInShipRoom || __instance.isInElevator || (__instance.playerHeldBy != null && (__instance.playerHeldBy.isInHangarShipRoom || __instance.playerHeldBy.isInElevator)) || StartOfRound.Instance.inShipPhase || RoundManager.Instance.mapPropsContainer == null)
                ___sprayPaintMask |= placeableShipObjects;
            else
                ___sprayPaintMask &= ~placeableShipObjects;

            // force spray paint decals to appear if framerate is too low
            if (___addSprayPaintWithFrameDelay > 0)
            {
                ___addSprayPaintWithFrameDelay = 0;
                ___delayedSprayPaintDecal.enabled = true;
            }
        }

        [HarmonyPatch(typeof(SprayPaintItem), "AddSprayPaintLocal")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> TransAddSprayPaintLocal(IEnumerable<CodeInstruction> instructions)
        {
            if (Compatibility.DISABLE_SPRAY_PAINT_PATCHES)
                return instructions;

            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 1; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0.175f && codes[i - 1].opcode == OpCodes.Call && codes[i - 1].operand.ToString().Contains("Distance"))
                {
                    codes[i].operand = 0.1f;
                    Plugin.Logger.LogDebug("Transpiler (Spray paint): Decrease distance between decals");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Spray paint transpiler failed");
            return codes;
        }
    }
}
