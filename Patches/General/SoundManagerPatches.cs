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
            // fix persistent effects when you disconnect and re-enter the game
            // TZP
            __instance.currentMixerSnapshotID = 4;
            __instance.SetDiageticMixerSnapshot(0, 0.2f);
            // mineshaft echo
            __instance.echoEnabled = true;
            __instance.SetEchoFilter(false);
        }
    }
}
