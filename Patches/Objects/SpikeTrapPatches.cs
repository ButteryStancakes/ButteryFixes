using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class SpikeTrapPatches
    {
        [HarmonyPatch(typeof(SpikeRoofTrap), "StickBodyToSpikes")]
        [HarmonyPostfix]
        static void PostStickBodyToSpikes(DeadBodyInfo body)
        {
            if (!Compatibility.DISABLE_PLAYERMODEL_PATCHES)
                body.MakeCorpseBloody();
        }
    }
}
