using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(MenuManager))]
    class MenuManagerPatches
    {
        [HarmonyPatch(nameof(MenuManager.Start))]
        [HarmonyPostfix]
        [HarmonyAfter(Compatibility.GUID_MORE_COMPANY)]
        static void MenuManager_Post_Start(MenuManager __instance)
        {
            if (Compatibility.INSTALLED_MORE_COMPANY)
            {
                HDAdditionalCameraData uiCamera = GameObject.Find("/UICamera")?.GetComponent<HDAdditionalCameraData>();
                if (uiCamera != null)
                {
                    foreach (HDAdditionalCameraData cam in Object.FindObjectsByType<HDAdditionalCameraData>(FindObjectsSortMode.None))
                    {
                        if (cam != uiCamera)
                        {
                            cam.volumeLayerMask = uiCamera.volumeLayerMask;
                            Plugin.Logger.LogDebug($"Menu: Override volumes on camera {cam.name}");
                        }
                    }
                }
            }
        }
    }
}
