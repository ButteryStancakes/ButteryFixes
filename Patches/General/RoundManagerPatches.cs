using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.General
{
    internal class RoundManagerPatches
    {
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.PredictAllOutsideEnemies))]
        [HarmonyPostfix]
        static void PostPredictAllOutsideEnemies()
        {
            // cleans up leftover spawn numbers from previous day + spawn predictions (when generating nests)
            foreach (string name in GlobalReferences.allEnemiesList.Keys)
                GlobalReferences.allEnemiesList[name].numberSpawned = 0;
        }

        [HarmonyPatch(typeof(RoundManager), "Start")]
        [HarmonyPostfix]
        static void RoundManagerPostStart(RoundManager __instance)
        {
            // prevents persistence when quitting mid-day and rehosting
            __instance.ResetEnemyVariables();
        }

        // is it a bug that the breaker box hums after unplugging the apparatus? I'm not too sure
        /*[HarmonyPatch(typeof(RoundManager), nameof(RoundManager.PowerSwitchOffClientRpc))]
        [HarmonyPostfix]
        static void PostPowerSwitchOffClientRpc()
        {
            Object.FindObjectOfType<BreakerBox>()?.breakerBoxHum.Stop();
        }*/
    }
}
