using HarmonyLib;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(BaboonBirdAI))]
    static class BaboonPatches
    {
        [HarmonyPatch(nameof(BaboonBirdAI.DoAIInterval))]
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

        [HarmonyPatch(nameof(BaboonBirdAI.Start))]
        [HarmonyPostfix]
        [HarmonyAfter(Compatibility.GUID_CRUISER_IMPROVED)]
        static void BaboonBirdAI_Post_Start(BaboonBirdAI __instance)
        {
            if (Compatibility.INSTALLED_CRUISER_IMPROVED && __instance.enemyType.SizeLimit == NavSizeLimit.NoLimit)
            {
                __instance.enemyType.SizeLimit = NavSizeLimit.MediumSpaces;
                Plugin.Logger.LogDebug("Fixed navigation for baboon hawk (rolled back Cruiser Improved patch)");
            }
        }
    }
}
