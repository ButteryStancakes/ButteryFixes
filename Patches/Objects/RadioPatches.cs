using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(WalkieTalkie))]
    static class RadioPatches
    {
        [HarmonyPatch(nameof(WalkieTalkie.EnableWalkieTalkieListening))]
        [HarmonyPostfix]
        static void WalkieTalkie_Post_EnableWalkieTalkieListening(WalkieTalkie __instance, bool enable)
        {
            if (enable || GameNetworkManager.Instance.localPlayerController.isPlayerDead || WalkieTalkie.allWalkieTalkies == null || WalkieTalkie.allWalkieTalkies.Count < 1)
                return;

            foreach (WalkieTalkie walkieTalkie in WalkieTalkie.allWalkieTalkies)
            {
                if (walkieTalkie == null || walkieTalkie == __instance || walkieTalkie.playerHeldBy != GameNetworkManager.Instance.localPlayerController)
                    continue;

                if (walkieTalkie.isBeingUsed)
                {
                    GameNetworkManager.Instance.localPlayerController.holdingWalkieTalkie = true;
                    StartOfRound.Instance.UpdatePlayerVoiceEffects();
                    Plugin.Logger.LogDebug("Player was about to lose radio privilege, while still holding a powered walkie!");
                    return;
                }
            }
        }
    }
}
