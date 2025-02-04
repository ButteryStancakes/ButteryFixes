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
            // skip search if a point is already cached
            // this probably won't break anything?
            if (__instance.exitPoint != null && __instance.exitPointAudio != null)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}
