using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class LandminePatches
    {
        [HarmonyPatch(typeof(Landmine), nameof(Landmine.Detonate))]
        [HarmonyPostfix]
        static void LandminePostDetonate(Landmine __instance)
        {
            Renderer scanSphere = __instance.transform.parent?.Find("ScanSphere")?.GetComponent<Renderer>();
            if (scanSphere != null)
            {
                scanSphere.forceRenderingOff = true;
                Plugin.Logger.LogDebug("Landmine: Hide radar dot after detonation");
            }
            if (!Compatibility.INSTALLED_GENERAL_IMPROVEMENTS)
            {
                ScanNodeProperties scanNodeProperties = __instance.transform.parent?.GetComponentInChildren<ScanNodeProperties>();
                if (scanNodeProperties != null)
                {
                    scanNodeProperties.GetComponent<Collider>().enabled = false;
                    Plugin.Logger.LogDebug("Landmine: Hide scan node after detonation");
                }
                if (__instance.TryGetComponent(out TerminalAccessibleObject terminalAccessibleObject))
                {
                    Object.Destroy(terminalAccessibleObject);
                    Plugin.Logger.LogDebug("Landmine: Hide terminal code after detonation");
                }
            }
        }

        [HarmonyPatch(typeof(Landmine), nameof(Landmine.SetOffMineAnimation))]
        [HarmonyPrefix]
        static bool PreSetOffMineAnimation(Landmine __instance)
        {
            Renderer rend = __instance.GetComponent<Renderer>();
            return !__instance.hasExploded || rend == null || rend.enabled;
        }
    }
}
