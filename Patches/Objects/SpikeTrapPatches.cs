using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(SpikeRoofTrap))]
    internal class SpikeTrapPatches
    {
        [HarmonyPatch(nameof(SpikeRoofTrap.StickBodyToSpikes))]
        [HarmonyPostfix]
        static void PostStickBodyToSpikes(DeadBodyInfo body)
        {
            if (Configuration.playermodelPatches.Value)
                body.MakeCorpseBloody();
        }
    }
}
