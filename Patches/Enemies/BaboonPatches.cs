using HarmonyLib;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    class BaboonPatches
    {
        [HarmonyPatch(typeof(BaboonBirdAI), nameof(BaboonBirdAI.DoAIInterval))]
        [HarmonyPrefix]
        static void BaboonBirdAI_Pre_DoAIInterval(BaboonBirdAI __instance)
        {
            // fixes potential nullref
            if (__instance.currentBehaviourStateIndex == 2 && __instance.focusingOnThreat && __instance.focusedThreat != null && __instance.focusedThreat.threatScript == null)
            {
                Plugin.Logger.LogWarning($"Baboon #{__instance.GetInstanceID()} is trying to focus on a threat (type \"{__instance.focusedThreat.type}\") that doesn't exist, this would've caused errors in vanilla");
                __instance.StopFocusingThreat();
            }
        }
    }
}
