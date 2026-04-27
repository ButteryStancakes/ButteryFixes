using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(GiftBoxItem))]
    static class GiftPatches
    {
        [HarmonyPatch(nameof(GiftBoxItem.InitializeAfterPositioning))]
        [HarmonyPostfix]
        static void GrabbableObject_Post_InitializeAfterPositioning(GiftBoxItem __instance)
        {
            if (!__instance.loadedItemFromSave && __instance.IsServer)
                ScrapTracker.TrackGiftBoxOnServer(__instance);
        }

        [HarmonyPatch(nameof(GiftBoxItem.OpenGiftBoxServerRpc))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> GiftBoxItem_Trans_OpenGiftBoxServerRpc(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 2; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand as MethodInfo == ReflectionCache.SET_SCRAP_VALUE)
                {
                    codes.InsertRange(i + 1, [
                        new(OpCodes.Ldarg_0),
                        new(codes[i - 2].opcode, codes[i - 2].operand),
                        new(OpCodes.Call, ReflectionCache.TRACK_GIFT_BOX_ON_CLIENT),
                    ]);
                    Plugin.Logger.LogDebug($"Transpiler (Gift box server): Handle scrap tracker");
                    return codes;
                }
            }

            Plugin.Logger.LogWarning($"Gift box server transpiler failed");
            return instructions;
        }

        [HarmonyPatch(nameof(GiftBoxItem.waitForGiftPresentToSpawnOnClient), MethodType.Enumerator)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> GiftBoxItem_Trans_waitForGiftPresentToSpawnOnClient(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 3; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand as MethodInfo == ReflectionCache.SET_SCRAP_VALUE)
                {
                    codes.InsertRange(i + 1, [
                        new(OpCodes.Ldarg_0),
                        new(codes[i - 3].opcode, codes[i - 3].operand),
                        new(OpCodes.Call, ReflectionCache.TRACK_GIFT_BOX_ON_CLIENT),
                    ]);
                    Plugin.Logger.LogDebug($"Transpiler (Gift box client): Handle scrap tracker");
                    return codes;
                }
            }

            Plugin.Logger.LogWarning($"Gift box client transpiler failed");
            return instructions;
        }
    }
}
