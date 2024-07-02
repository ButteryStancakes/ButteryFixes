using HarmonyLib;

namespace ButteryFixes.Patches.Items
{
    internal class ApparatusPatches
    {
        [HarmonyPatch(typeof(LungProp), nameof(LungProp.Start))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        static void LungPropPostStart(LungProp __instance)
        {
            ScanNodeProperties scanNodeProperties = __instance.GetComponentInChildren<ScanNodeProperties>();
            if (scanNodeProperties != null)
            {
                if (scanNodeProperties.headerText == "Apparatice")
                    scanNodeProperties.headerText = "Apparatus";
                scanNodeProperties.subText = $"Value: ${__instance.scrapValue}";
                Plugin.Logger.LogInfo("Scan node: Apparatus");
            }
        }
    }
}
