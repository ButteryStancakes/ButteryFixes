using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(TimeOfDay))]
    internal class TimeOfDayPatches
    {
        [HarmonyPatch(nameof(TimeOfDay.PlayTimeMusicDelayed))]
        [HarmonyPrefix]
        static void TimeOfDay_Pre_PlayTimeMusicDelayed(TimeOfDay __instance)
        {
            // allow music to play again, unless visiting Gordion multiple times in a row
            if (StartOfRound.Instance.currentLevel.name != "CompanyBuildingLevel" && !__instance.TimeOfDayMusic.isPlaying && !SoundManager.Instance.musicSource.isPlaying)
                __instance.playDelayedMusicCoroutine = null;
        }

        [HarmonyPatch(nameof(TimeOfDay.OnDayChanged))]
        [HarmonyAfter(Compatibility.GUID_YES_FOX)]
        [HarmonyPostfix]
        static void TimeOfDay_Post_OnDayChanged(StartOfRound __instance)
        {
            NonPatchFunctions.TestForVainShrouds();
        }
    }
}
