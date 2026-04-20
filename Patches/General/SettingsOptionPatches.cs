using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(SettingsOption))]
    static class SettingsOptionPatches
    {
        [HarmonyPatch(nameof(SettingsOption.OnEnable))]
        [HarmonyPostfix]
        static void SettingsOption_Post_OnEnable(SettingsOption __instance)
        {
            if (__instance.optionType == SettingsOptionType.PixelRes)
            {
                TMP_Dropdown dropdown = __instance.GetComponentInChildren<TMP_Dropdown>();
                if (dropdown != null)
                {
                    if (!dropdown.options.Any(option => option.text == "High"))
                    {
                        // add "High" setting
                        List<TMP_Dropdown.OptionData> options = new(dropdown.options);
                        options.Insert(0, new("High"));
                        dropdown.ClearOptions();
                        dropdown.AddOptions(options);
                    }

                    // now update with the adjusted index
                    try
                    {
                        GlobalReferences.forceMaxQuality = ES3.Load("ButteryFixes_ForceMaxQuality", "LCGeneralSaveData", false);
                    }
                    catch (System.Exception e)
                    {
                        Plugin.Logger.LogError($"An error occurred while trying to read settings from file \"LCGeneralSaveData\"");
                        Plugin.Logger.LogError(e);
                    }
                    dropdown.SetValueWithoutNotify(GlobalReferences.forceMaxQuality ? 0 : IngamePlayerSettings.Instance.settings.pixelRes + 1);
                }
            }
        }

        [HarmonyPatch(nameof(SettingsOption.SetValueToMatchSettings))]
        [HarmonyPrefix]
        static bool SettingsOption_Pre_SetValueToMatchSettings(SettingsOption __instance)
        {
            if (__instance.optionType == SettingsOptionType.PixelRes)
            {
                TMP_Dropdown dropdown = __instance.GetComponentInChildren<TMP_Dropdown>();
                if (dropdown == null)
                    return true;

                if (GlobalReferences.forceMaxQuality)
                    dropdown.SetValueWithoutNotify(0);
                else if (dropdown.options.Count - 1 > IngamePlayerSettings.Instance.settings.pixelRes)
                    dropdown.SetValueWithoutNotify(IngamePlayerSettings.Instance.settings.pixelRes + 1);
                else
                    Plugin.Logger.LogError("Resolution dropdown is missing a setting - this shouldn't happen");

                return false;
            }

            return true;
        }
    }
}