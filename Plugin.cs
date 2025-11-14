using BepInEx;
using BepInEx.Logging;
using ButteryFixes.Utility;
using HarmonyLib;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ButteryFixes
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency(Compatibility.GUID_GENERAL_IMPROVEMENTS, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_LETHAL_QUANTITIES, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_MORE_COMPANY, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_EVERYTHING_CAN_DIE, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_TOUCHSCREEN, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_REBALANCED_MOONS, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_LETHAL_FIXES, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_LOBBY_COMPATIBILITY, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_YES_FOX, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_OPEN_BODY_CAMS, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_NO_LOST_SIGNAL, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Compatibility.GUID_DROP_SHIP_DELIVERY_CAP_MODIFIER, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        internal const string PLUGIN_GUID = "butterystancakes.lethalcompany.butteryfixes", PLUGIN_NAME = "Buttery Fixes", PLUGIN_VERSION = "1.15.5";
        internal static new ManualLogSource Logger;

        void Awake()
        {
            Logger = base.Logger;

            Compatibility.Init();
            Configuration.Init(Config);

            if (RestoreFilmGrain.GetTextures())
                SceneManager.sceneLoaded += RestoreFilmGrain.OverrideVolumes;

            new Harmony(PLUGIN_GUID).PatchAll();

            SceneManager.sceneLoaded += SceneOverrides.OnSceneLoaded;
            SceneManager.sceneUnloaded += delegate
            {
                GlobalReferences.caveTiles.Clear();
            };

            RenderPipelineManager.beginCameraRendering += RenderingOverrides.OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += RenderingOverrides.OnEndCameraRendering;

            Logger.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded");
        }
    }
}