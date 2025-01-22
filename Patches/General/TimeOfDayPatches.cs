using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch]
    internal class TimeOfDayPatches
    {
        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.PlayTimeMusicDelayed))]
        [HarmonyPrefix]
        static void PostPredictAllOutsideEnemies(TimeOfDay __instance, ref Coroutine ___playDelayedMusicCoroutine)
        {
            // allow music to play again, unless visiting Gordion multiple times in a row
            if (StartOfRound.Instance.currentLevel.name != "CompanyBuildingLevel" && !__instance.TimeOfDayMusic.isPlaying && !SoundManager.Instance.musicSource.isPlaying)
                ___playDelayedMusicCoroutine = null;
        }
    }
}
