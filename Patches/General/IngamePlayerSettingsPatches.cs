using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(IngamePlayerSettings))]
    static class IngamePlayerSettingsPatches
    {
        [HarmonyPatch(nameof(IngamePlayerSettings.SetPixelResolution))]
        [HarmonyPostfix]
        static void IngamePlayerSettings_Post_SetPixelResolution(IngamePlayerSettings __instance)
        {
            if (Configuration.forceMaxQuality.Value && GlobalReferences.Terminal != null)
            {
                __instance.playerGameplayScreenTex.Release();
                __instance.playerGameplayScreenTex.width = GlobalReferences.Terminal.playerScreenTexHighRes.width;
                __instance.playerGameplayScreenTex.height = GlobalReferences.Terminal.playerScreenTexHighRes.height;
                Plugin.Logger.LogInfo("High resolution applied");
            }
        }
    }
}