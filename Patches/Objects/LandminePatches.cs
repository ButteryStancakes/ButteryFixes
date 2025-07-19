using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(Landmine))]
    internal class LandminePatches
    {
        [HarmonyPatch(nameof(Landmine.Start))]
        [HarmonyPostfix]
        static void Landmine_Post_Start(Landmine __instance)
        {
            Transform mapDot = __instance.transform.parent?.Find("ScanSphere");
            if (mapDot != null)
            {
                mapDot.position += Vector3.up;
                Plugin.Logger.LogDebug($"Landmine #{__instance.GetInstanceID()}: Nudged radar dot upwards");
            }
        }

        [HarmonyPatch(nameof(Landmine.Detonate))]
        [HarmonyPostfix]
        static void Landmine_Post_Detonate(Landmine __instance)
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

        [HarmonyPatch(nameof(Landmine.SetOffMineAnimation))]
        [HarmonyPrefix]
        static bool Landmine_Pre_SetOffMineAnimation(Landmine __instance)
        {
            Renderer rend = __instance.GetComponent<Renderer>();
            return !__instance.hasExploded || rend == null || rend.enabled;
        }
    }
}
