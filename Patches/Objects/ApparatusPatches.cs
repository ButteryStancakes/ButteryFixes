using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(LungProp))]
    internal class ApparatusPatches
    {
        [HarmonyPatch(nameof(LungProp.Start))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        static void LungProp_Post_Start(LungProp __instance)
        {
            ScanNodeProperties scanNodeProperties = __instance.GetComponentInChildren<ScanNodeProperties>();
            if (scanNodeProperties != null)
            {
                if (scanNodeProperties.headerText == "Apparatice")
                    scanNodeProperties.headerText = "Apparatus";
                if (Configuration.showApparatusValue.Value)
                {
                    scanNodeProperties.scrapValue = __instance.scrapValue;
                    scanNodeProperties.subText = $"Value: ${scanNodeProperties.scrapValue}";
                }
                Plugin.Logger.LogDebug("Scan node: Apparatus");
            }
        }
    }
}
