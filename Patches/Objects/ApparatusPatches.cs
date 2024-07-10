using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
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
                if (Plugin.configShowApparatusValue.Value)
                {
                    scanNodeProperties.scrapValue = __instance.scrapValue;
                    scanNodeProperties.subText = $"Value: ${scanNodeProperties.scrapValue}";
                }
                Plugin.Logger.LogInfo("Scan node: Apparatus");
            }
        }
    }
}
