using HarmonyLib;

namespace ButteryFixes.Patches.Items
{
    [HarmonyPatch]
    internal class ItemPatches
    {
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.ChargeBatteries))]
        [HarmonyPostfix]
        static void PostChargeBatteries(GrabbableObject __instance)
        {
            if (__instance is BoomboxItem)
            {
                BoomboxItem boomboxItem = __instance as BoomboxItem;
                // needs to verify charge is > 0 because there's a special pitch effect on battery death we don't want to interrupt
                if (boomboxItem.isPlayingMusic && boomboxItem.boomboxAudio.pitch < 1f && boomboxItem.insertedBattery.charge > 0f)
                {
                    boomboxItem.boomboxAudio.pitch = 1f;
                    Plugin.Logger.LogInfo("Boombox was recharged, correcting pitch");
                }
            }
        }
    }
}
