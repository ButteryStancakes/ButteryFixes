using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch]
    internal class EntranceTeleportPatches
    {
        [HarmonyPatch(typeof(EntranceTeleport), nameof(EntranceTeleport.FindExitPoint))]
        [HarmonyPrefix]
        static bool EntranceTeleport_Pre_FindExitPoint(EntranceTeleport __instance, ref bool __result)
        {
            // fix fire exit bug
            if (__instance.entranceId > 0 && !GlobalReferences.exitIDsSet)
            {
                if (!GlobalReferences.needToFetchExitPoints)
                {
                    Plugin.Logger.LogWarning("EntranceTeleport.FindExitPoint() was called before RoundManager.SetExitIDs(), which is very likely to cause problems. Either a mod is doing this or you are experiencing excessive load times");
                    GlobalReferences.needToFetchExitPoints = true;
                }
                __result = false;
                return false;
            }

            // skip search if a point is already cached
            if (__instance.exitPoint != null && __instance.exitPointAudio != null)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}
