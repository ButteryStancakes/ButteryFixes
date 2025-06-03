using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(SprayPaintItem))]
    class SprayPaintPatches
    {
        [HarmonyPatch(nameof(SprayPaintItem.LateUpdate))]
        [HarmonyPrefix]
        static void SprayPaintItem_Pre_LateUpdate(SprayPaintItem __instance)
        {
            if (__instance.isSpraying)
            {
                if (__instance.itemProperties.canBeInspected && __instance.playerHeldBy != null && __instance.playerHeldBy.IsInspectingItem)
                {
                    __instance.isSpraying = false;
                    __instance.StopSpraying();
                }
                // make painting sequential decals smoother
                else if (!Compatibility.DISABLE_SPRAY_PAINT_PATCHES && !__instance.isWeedKillerSprayBottle && __instance.sprayInterval > 0f && __instance.delayedSprayPaintDecal != null && __instance.delayedSprayPaintDecal.enabled && __instance.sprayInterval < (__instance.sprayIntervalSpeed - Time.fixedDeltaTime))
                    __instance.sprayInterval = 0f;
            }
        }

        [HarmonyPatch(nameof(SprayPaintItem.ItemActivate))]
        [HarmonyPrefix]
        static void SprayPaintItem_Pre_ItemActivate(SprayPaintItem __instance, ref bool buttonDown)
        {
            if (buttonDown && __instance.itemProperties.canBeInspected && __instance.playerHeldBy != null && __instance.playerHeldBy.IsInspectingItem && __instance.sprayCanTank > 0f)
                buttonDown = false;
        }

        [HarmonyPatch(nameof(SprayPaintItem.EquipItem))]
        [HarmonyPostfix]
        static void SprayPaintItem_Post_EquipItem(SprayPaintItem __instance)
        {
            // can't shake weed killer
            if (__instance.isWeedKillerSprayBottle)
                __instance.playerHeldBy.equippedUsableItemQE = false;
        }

        [HarmonyPatch(nameof(SprayPaintItem.Start))]
        [HarmonyPostfix]
        static void SprayPaintItem_Post_Start(SprayPaintItem __instance)
        {
            if (!__instance.isWeedKillerSprayBottle)
            {
                if (!Compatibility.DISABLE_SPRAY_PAINT_PATCHES)
                {
                    __instance.sprayIntervalSpeed = 0.037f; // 0.05
                    // 600 * (0.08 / 0.037) * (0.175 / 0.1) = 2270, rounded to multiple of 200
                    __instance.maxSprayPaintDecals = 2200;
                }
                if (!Compatibility.INSTALLED_GENERAL_IMPROVEMENTS)
                {
                    __instance.sprayCanMatsIndex = new System.Random((int)__instance.NetworkObjectId).Next(__instance.particleMats.Length);
                    __instance.sprayParticle.GetComponent<ParticleSystemRenderer>().material = __instance.particleMats[__instance.sprayCanMatsIndex];
                    __instance.sprayCanNeedsShakingParticle.GetComponent<ParticleSystemRenderer>().material = __instance.particleMats[__instance.sprayCanMatsIndex];
                    Plugin.Logger.LogDebug($"Rerolled spray can #{__instance.NetworkObjectId} color");
                }
            }
        }

        [HarmonyPatch(nameof(SprayPaintItem.AddSprayPaintLocal))]
        [HarmonyPrefix]
        static void SprayPaintItem_Pre_AddSprayPaintLocal(SprayPaintItem __instance)
        {
            if (Compatibility.DISABLE_SPRAY_PAINT_PATCHES)
                return;

            // don't collide with player (same bug as jetpack)
            int placeableShipObjects = (1 << 26);
            if (__instance.isInShipRoom || __instance.isInElevator || (__instance.playerHeldBy != null && (__instance.playerHeldBy.isInHangarShipRoom || __instance.playerHeldBy.isInElevator)) || StartOfRound.Instance.inShipPhase || RoundManager.Instance.mapPropsContainer == null)
                __instance.sprayPaintMask |= placeableShipObjects;
            else
                __instance.sprayPaintMask &= ~placeableShipObjects;

            // force spray paint decals to appear if framerate is too low
            if (__instance.addSprayPaintWithFrameDelay > 0)
            {
                __instance.addSprayPaintWithFrameDelay = 0;
                __instance.delayedSprayPaintDecal.enabled = true;
            }
        }

        [HarmonyPatch(nameof(SprayPaintItem.AddSprayPaintLocal))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> SprayPaintItem_Trans_AddSprayPaintLocal(IEnumerable<CodeInstruction> instructions)
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
            return instructions;
        }
    }
}
