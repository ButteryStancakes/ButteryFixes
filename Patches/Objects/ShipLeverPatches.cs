using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    class ShipLeverPatches
    {
        [HarmonyPatch(typeof(StartMatchLever), nameof(StartMatchLever.PlayLeverPullEffectsClientRpc))]
        [HarmonyPrefix]
        static void StartMatchLever_Pre_PlayLeverPullEffectsClientRpc(StartMatchLever __instance, ref bool __state)
        {
            // clientSentRPC means local player pulled the lever
            if (__instance.clientSentRPC && Configuration.lockInTerminal.Value)
                __instance.StartCoroutine(NonPatchFunctions.InteractionTemporarilyLocksCamera(__instance.triggerScript));
        }
    }
}
