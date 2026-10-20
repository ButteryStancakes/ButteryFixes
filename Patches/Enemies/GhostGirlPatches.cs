using ButteryFixes.Utility;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(DressGirlAI))]
    static class GhostGirlPatches
    {
        static bool chasing;

        [HarmonyPatch(nameof(DressGirlAI.BeginChasing))]
        [HarmonyPrefix]
        static void DressGirlAI_Pre_BeginChasing(DressGirlAI __instance)
        {
            if (__instance.IsOwner)
                chasing = true;
        }

        [HarmonyPatch(nameof(DressGirlAI.BeginChasing))]
        [HarmonyPostfix]
        static void DressGirlAI_Post_BeginChasing(DressGirlAI __instance)
        {
            if (chasing)
            {
                RoundManager.Instance.FlickerLights(true, true);
                GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(__instance.timesSeenByPlayer > 0 ? 0.9f : 0.2f);
            }
            chasing = false;
        }

        [HarmonyPatch(nameof(DressGirlAI.FlipLightsBreakerServerRpc))]
        [HarmonyPostfix]
        static void DressGirlAI_Post_FlipLightsBreakerServerRpc(DressGirlAI __instance)
        {
            chasing = false;
        }

        [HarmonyPatch(nameof(DressGirlAI.Update))]
        [HarmonyPrefix]
        static void DressGirlAI_Pre_Update()
        {
            GlobalReferences.girlUpdating = true;
        }

        [HarmonyPatch(nameof(DressGirlAI.Update))]
        [HarmonyPostfix]
        static void DressGirlAI_Post_Update(DressGirlAI __instance)
        {
            GlobalReferences.girlUpdating = false;
            if (GlobalReferences.girlRenderer != null && __instance == GlobalReferences.girl)
            {
                bool dontRender = !__instance.IsOwner || GlobalReferences.girlRenderer.gameObject.layer == 23;
                if (GlobalReferences.girlRenderer.forceRenderingOff != dontRender)
                    GlobalReferences.girlRenderer.forceRenderingOff = dontRender;
                if (dontRender && __instance.creatureAnimator.GetBool("Walk"))
                    __instance.creatureAnimator.SetBool("Walk", false);
            }
        }

        [HarmonyPatch(nameof(DressGirlAI.Start))]
        [HarmonyPostfix]
        static void DressGirlAI_Post_Start(DressGirlAI __instance)
        {
            if (GlobalReferences.girl == null)
            {
                GlobalReferences.girl = __instance;
                if (GlobalReferences.girlRenderer == null)
                {
                    foreach (SkinnedMeshRenderer skinnedMeshRenderer in __instance.skinnedMeshRenderers)
                    {
                        if (skinnedMeshRenderer.name == "basemesh")
                        {
                            GlobalReferences.girlRenderer = skinnedMeshRenderer;
                            break;
                        }
                    }
                }
            }
            else
                Plugin.Logger.LogWarning("2 ghost girls have spawned at the same time, this might cause glitchy behavior");

            foreach (MeshRenderer meshRenderer in __instance.meshRenderers)
            {
                if (meshRenderer.name.StartsWith("Icosphere"))
                {
                    meshRenderer.forceRenderingOff = true;
                    meshRenderer.gameObject.SetActive(false);
                }
            }
        }
    }
}
