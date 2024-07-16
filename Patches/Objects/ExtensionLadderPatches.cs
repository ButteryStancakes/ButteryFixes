using HarmonyLib;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class ExtensionLadderPatches
    {
        [HarmonyPatch(typeof(ExtensionLadderItem), "StartLadderAnimation")]
        [HarmonyPostfix]
        static void PostStartLadderAnimation(ref bool ___ladderBlinkWarning)
        {
            if (___ladderBlinkWarning)
            {
                ___ladderBlinkWarning = false;
                Plugin.Logger.LogInfo("Fixed broken extension ladder warning");
            }
        }
    }
}
