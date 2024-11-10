using BepInEx;
using BepInEx.Logging;
using ButteryFixes.Utility;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace ButteryFixes
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency(Compatibility.GUID_FAST_CLIMBING, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_BETTER_LADDERS, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_GENERAL_IMPROVEMENTS, BepInDependency.DependencyFlags.SoftDependency)]
    //[BepInDependency(Compatibility.GUID_LETHAL_FIXES, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_MODEL_REPLACEMENT_API, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_STARLANCER_AI_FIX, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_LETHAL_QUANTITIES, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_EVERYTHING_CAN_DIE, BepInDependency.DependencyFlags.SoftDependency)]
    //[BepInDependency(Compatibility.GUID_MORE_COMPANY, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_ARTIFICE_BLIZZARD, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_CELESTIAL_TINT, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        const string PLUGIN_GUID = "butterystancakes.lethalcompany.butteryfixes", PLUGIN_NAME = "Buttery Fixes", PLUGIN_VERSION = "1.10.8";
        internal static new ManualLogSource Logger;

        void Awake()
        {
            Logger = base.Logger;

            Compatibility.Init();
            Configuration.Init(Config);

            new Harmony(PLUGIN_GUID).PatchAll();

            SceneManager.sceneLoaded += SceneOverrides.OnSceneLoaded;

            Logger.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded");
        }
    }
}