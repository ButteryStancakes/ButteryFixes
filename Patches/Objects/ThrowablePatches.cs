using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(StunGrenadeItem))]
    internal class ThrowablePatches
    {
        [HarmonyPatch(nameof(StunGrenadeItem.Start))]
        [HarmonyPostfix]
        static void StunGrenadeItem_Post_Start(StunGrenadeItem __instance)
        {
            __instance.stunGrenadeMask |= 1 << LayerMask.NameToLayer("Catwalk");
        }
    }
}
