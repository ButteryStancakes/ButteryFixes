using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(KiwiBabyItem))]
    internal class BabyBirdPatches
    {
        [HarmonyPatch(nameof(KiwiBabyItem.Start))]
        [HarmonyPostfix]
        static void KiwiBabyItem_Post_Start(KiwiBabyItem __instance)
        {
            if (__instance.takenIntoOrbit && GlobalReferences.StartMatchLever != null && GlobalReferences.StartMatchLever.leverHasBeenPulled)
            {
                Plugin.Logger.LogWarning("Sapsucker eggs detected they were in orbit immediately upon spawn. Unless you joined a lobby that is currently in orbit, this should never happen. If the lever was just pulled, expect the eggs to prematurely hatch!");
                __instance.takenIntoOrbit = false;
                __instance.currentAnimation = 0;
            }
        }
    }
}
