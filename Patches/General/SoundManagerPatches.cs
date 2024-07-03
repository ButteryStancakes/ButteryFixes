using HarmonyLib;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch]
    internal class SoundManagerPatches
    {
        [HarmonyPatch(typeof(SoundManager), "Start")]
        [HarmonyPostfix]
        static void SoundManagerPostStart(SoundManager __instance)
        {
            // fixes the TZP effects persisting when you disconnect and re-enter the game
            __instance.currentMixerSnapshotID = 4;
            __instance.SetDiageticMixerSnapshot(0, 0.2f);
        }
    }
}
