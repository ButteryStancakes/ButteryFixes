using ButteryFixes.Utility;
using HarmonyLib;
using UnityEngine.Rendering.HighDefinition;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(IngamePlayerSettings))]
    static class IngamePlayerSettingsPatches
    {
        [HarmonyPatch(nameof(IngamePlayerSettings.SetPixelResolution))]
        [HarmonyPrefix]
        static bool IngamePlayerSettings_Pre_SetPixelResolution(IngamePlayerSettings __instance)
        {
            if (!GlobalReferences.forceMaxQuality)
                return true;

            __instance.unsavedSettings.pixelRes = 0;

            // new "High" setting
            __instance.playerGameplayScreenTex.Release();
            if (GlobalReferences.Terminal != null)
            {
                __instance.playerGameplayScreenTex.width = GlobalReferences.Terminal.playerScreenTexHighRes.width;
                __instance.playerGameplayScreenTex.height = GlobalReferences.Terminal.playerScreenTexHighRes.height;
            }
            else
            {
                __instance.playerGameplayScreenTex.width = 970;
                __instance.playerGameplayScreenTex.height = 580;
            }
            Plugin.Logger.LogInfo("High resolution applied");

            return false;
        }

        [HarmonyPatch(nameof(IngamePlayerSettings.LoadSettingsFromPrefs))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        static void IngamePlayerSettings_Post_LoadSettingsFromPrefs()
        {
            GlobalReferences.forceMaxQuality = ES3.Load("ButteryFixes_ForceMaxQuality", "LCGeneralSaveData", false);
        }

        [HarmonyPatch(nameof(IngamePlayerSettings.SaveSettingsToPrefs))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        static void IngamePlayerSettings_Post_SaveSettingsToPrefs()
        {
            ES3.Save("ButteryFixes_ForceMaxQuality", GlobalReferences.forceMaxQuality, "LCGeneralSaveData");
        }

        [HarmonyPatch(nameof(IngamePlayerSettings.SetOption))]
        [HarmonyPrefix]
        static bool IngamePlayerSettings_Pre_SetOption(IngamePlayerSettings __instance, SettingsOptionType optionType, int value)
        {
            if (optionType == SettingsOptionType.PixelRes)
            {
                if (GameNetworkManager.Instance != null)
                    __instance.SettingsAudio.PlayOneShot(GameNetworkManager.Instance.buttonTuneSFX);
                __instance.SetChangesNotAppliedTextVisible(true);

                if (value == 0)
                {
                    GlobalReferences.forceMaxQuality = true;
                    __instance.SetPixelResolution(0);
                }
                else
                {
                    GlobalReferences.forceMaxQuality = false;
                    __instance.SetPixelResolution(value - 1);
                }

                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(IngamePlayerSettings.ResetSettingsToDefault))]
        [HarmonyPrefix]
        static void IngamePlayerSettings_Pre_ResetSettingsToDefault()
        {
            GlobalReferences.forceMaxQuality = false;
        }

        [HarmonyPatch(nameof(IngamePlayerSettings.DiscardChangedSettings))]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        static void IngamePlayerSettings_Pre_DiscardChangedSettings()
        {
            GlobalReferences.forceMaxQuality = ES3.Load("ButteryFixes_ForceMaxQuality", "LCGeneralSaveData", false);
        }

        [HarmonyPatch(nameof(IngamePlayerSettings.SetMotionBlur))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        static void IngamePlayerSettings_Post_SetMotionBlur(int value)
        {
            MotionBlur motionBlur;
            if (HUDManager.Instance?.insanityScreenFilter != null && HUDManager.Instance.insanityScreenFilter.sharedProfile.TryGet(out motionBlur))
            {
                motionBlur.active = value != 2;
                Plugin.Logger.LogInfo("Fear motion blur applied");
            }
        }
    }
}