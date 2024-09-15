using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class ThrowablePatches
    {
        [HarmonyPatch(typeof(StunGrenadeItem), nameof(StunGrenadeItem.Start))]
        [HarmonyPostfix]
        static void StunGrenadeItemPostStartStart(ref int ___stunGrenadeMask)
        {
            ___stunGrenadeMask |= 1 << LayerMask.NameToLayer("Catwalk");
        }
    }
}
