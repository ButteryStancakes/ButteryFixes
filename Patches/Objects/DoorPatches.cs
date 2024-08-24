using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class DoorPatches
    {
        [HarmonyPatch(typeof(DoorLock), "Awake")]
        [HarmonyPostfix]
        static void DoorLockPostAwake(DoorLock __instance, ref bool ___hauntedDoor)
        {
            if (!__instance.IsServer)
                ___hauntedDoor = false;
        }
    }
}
