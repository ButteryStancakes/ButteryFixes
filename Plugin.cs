using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;

namespace ButteryFixes
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("inoyu.FastClimbing", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("e3s1.BetterLadders", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        const string PLUGIN_GUID = "butterystancakes.lethalcompany.butteryfixes", PLUGIN_NAME = "Buttery Fixes", PLUGIN_VERSION = "1.0.2";
        internal static new ManualLogSource Logger;

        internal static bool DISABLE_LADDER_PATCH;

        void Awake()
        {
            Logger = base.Logger;

            if (Chainloader.PluginInfos.ContainsKey("inoyu.FastClimbing") || Chainloader.PluginInfos.ContainsKey("e3s1.BetterLadders"))
            {
                Logger.LogInfo("CROSS-COMPATIBILITY - Ladder patch will be disabled");
                DISABLE_LADDER_PATCH = true;
            }

            new Harmony(PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded");
        }
    }
}