using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch]
    class StormyWeatherPatches
    {
        [HarmonyPatch(typeof(StormyWeather), "Update")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> StormyWeatherTransUpdate(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo metalObjects = AccessTools.Field(typeof(StormyWeather), "metalObjects");
            FieldInfo isInFactory = AccessTools.Field(typeof(GrabbableObject), nameof(GrabbableObject.isInFactory));
            for (int i = 6; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Brtrue && codes[i - 1].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 1].operand == isInFactory && codes[i - 4].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 4].operand == metalObjects && codes[i - 3].opcode.ToString().StartsWith("ldloc") && codes[i - 2].ToString().Contains("get_Item"))
                {
                    codes.InsertRange(i - 5, [
                        new(OpCodes.Ldarg_0),
                        new(OpCodes.Ldfld, metalObjects),
                        new(codes[i - 3].opcode, codes[i - 3].operand), // should be ldloc.2
                        new(codes[i - 2].opcode, codes[i - 2].operand), // should be callvirt get_Item
                        new(OpCodes.Ldnull),
                        new(OpCodes.Call, AccessTools.Method(typeof(Object), "op_Inequality", [typeof(Object), typeof(Object)])),
                        new(OpCodes.Brfalse, codes[i].operand)
                    ]);
                    Plugin.Logger.LogDebug("Transpiler (Stormy weather): Null check when iterating metalObjects");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Stormy weather transpiler failed");
            return instructions;
        }

        [HarmonyPatch(typeof(StormyWeather), "Update")]
        [HarmonyPostfix]
        static void StormyWeatherPostUpdate(ref GrabbableObject ___targetingMetalObject, bool ___hasShownStrikeWarning, ref float ___getObjectToTargetInterval)
        {
            if (RoundManager.Instance.IsOwner && ___targetingMetalObject != null && !___hasShownStrikeWarning && StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(___targetingMetalObject.transform.position) && (___targetingMetalObject.playerHeldBy == null || (___targetingMetalObject.playerHeldBy.isInHangarShipRoom && StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(___targetingMetalObject.playerHeldBy.transform.position))))
            {
                Plugin.Logger.LogDebug($"Item {___targetingMetalObject.name} #{___targetingMetalObject.GetInstanceID()} was targeted by lightning but is inside the ship");
                PlayerControllerB player = ___targetingMetalObject.playerHeldBy ?? GameNetworkManager.Instance.localPlayerController;
                player.SetItemInElevator(true, true, ___targetingMetalObject);
                ___targetingMetalObject = null;
                // re-target sooner
                ___getObjectToTargetInterval = 3.86f; // every 0.14s
            }
        }
    }
}
