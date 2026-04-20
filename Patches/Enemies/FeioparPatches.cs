using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(PumaAI))]
    static class FeioparPatches
    {
        [HarmonyPatch(nameof(PumaAI.Start))]
        [HarmonyPostfix]
        static void PumaAI_Post_Start(PumaAI __instance)
        {
            foreach (Collider collider in new Collider[]
            {
                __instance.footstepSource?.GetComponent<Collider>(),
                __instance.growlSource?.GetComponent<Collider>(),
            })
            {
                if (collider != null)
                {
                    collider.isTrigger = true;
                    Plugin.Logger.LogDebug($"Puma #{__instance.GetInstanceID()}: Fixed erroneous collision on \"{collider.name}\"");
                }
            }
        }
    }
}
