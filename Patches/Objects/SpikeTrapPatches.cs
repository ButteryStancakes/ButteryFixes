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
    }
}
