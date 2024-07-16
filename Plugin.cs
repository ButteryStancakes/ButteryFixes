using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using ButteryFixes.Utility;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace ButteryFixes
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency(Compatibility.FAST_CLIMBING_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.BETTER_LADDERS_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GENERAL_IMPROVEMENTS_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.LETHAL_FIXES_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.MODEL_REPLACEMENT_API_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.STARLANCER_AI_FIX_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        const string PLUGIN_GUID = "butterystancakes.lethalcompany.butteryfixes", PLUGIN_NAME = "Buttery Fixes", PLUGIN_VERSION = "1.5.4";
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