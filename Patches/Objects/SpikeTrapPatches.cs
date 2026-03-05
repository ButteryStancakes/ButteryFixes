using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(SpikeRoofTrap))]
    internal class SpikeTrapPatches
    {
        [HarmonyPatch(nameof(SpikeRoofTrap.StickBodyToSpikes))]
        [HarmonyPostfix]
        static void SpikeRoofTrap_Post_StickBodyToSpikes(DeadBodyInfo body)
        {
            if (Configuration.playermodelPatches.Value)
                body.MakeCorpseBloody();
        }

        [HarmonyPatch(nameof(SpikeRoofTrap.SetRandomSpikeTrapAudioPitch))]
        [HarmonyPrefix]
        static void SpikeRoofTrap_Pre_SetRandomSpikeTrapAudioPitch(SpikeRoofTrap __instance, ref float __state)
        {
            __state = __instance.spikeTrapAudio.pitch;
        }

        [HarmonyPatch(nameof(SpikeRoofTrap.SetRandomSpikeTrapAudioPitch))]
        [HarmonyPostfix]
        static void SpikeRoofTrap_Post_SetRandomSpikeTrapAudioPitch(SpikeRoofTrap __instance, float __state)
        {
            if (__instance.spikeTrapAudio.isPlaying && __instance.spikeTrapAudio.pitch != __state)
                __instance.spikeTrapAudio.Stop();
        }
    }
}
