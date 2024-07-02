using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    internal class BarberPatches
    {
        // fix barbers not speeding up throughout the day
        [HarmonyPatch(typeof(ClaySurgeonAI), "HourChanged")]
        [HarmonyPrefix]
        static bool ClaySurgeonAIPreHourChanged(ClaySurgeonAI __instance)
        {
            // use float division instead of integer division to prevent truncation
            __instance.currentInterval = Mathf.Lerp(__instance.startingInterval, __instance.endingInterval, (float)TimeOfDay.Instance.hour / TimeOfDay.Instance.numberOfHours);

            // vanilla behavior doesn't work
            return false;
        }

        // fix barbers freezing after "spam-snipping" players
        [HarmonyPatch(typeof(ClaySurgeonAI), nameof(ClaySurgeonAI.KillPlayerClientRpc))]
        [HarmonyPostfix]
        static void ClaySurgeonAIPostKillPlayerClientRpc(ClaySurgeonAI __instance, ref float ___beatTimer, ref float ___snareIntervalTimer)
        {
            if (__instance.IsOwner && ___beatTimer > __instance.startingInterval + 2f)
            {
                ___beatTimer = __instance.startingInterval + 2f;
                //Plugin.Logger.LogDebug("Barber is going to freeze, reduce beat timer");
            }
            if (___snareIntervalTimer > ___beatTimer - __instance.snareOffset)
                ___snareIntervalTimer = ___beatTimer - __instance.snareOffset;
        }
    }
}
