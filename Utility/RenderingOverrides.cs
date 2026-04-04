using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.Rendering;

namespace ButteryFixes.Utility
{
    internal static class RenderingOverrides
    {
        static bool? armsNotRendering;

        public static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            ResetRendering();

            if (GlobalReferences.viewmodelArms == null)
            {
                armsNotRendering = null;
                return;
            }

            if (camera == GlobalReferences.shipCamera || camera == GlobalReferences.securityCamera)
            {
                armsNotRendering = GlobalReferences.viewmodelArms.forceRenderingOff;
                GlobalReferences.viewmodelArms.forceRenderingOff = true;
            }

            ManualCameraRenderer mapScreen = StartOfRound.Instance?.mapScreen;
            if (mapScreen != null && camera == mapScreen.headMountedCam && mapScreen.targetedPlayer != null)
            {
                if (mapScreen.targetedPlayer.isPlayerDead)
                {
                    if (mapScreen.targetedPlayer.deadBody?.nightVisionRadar != null)
                        mapScreen.targetedPlayer.deadBody.nightVisionRadar.enabled = true;
                }
                else if (mapScreen.targetedPlayer.nightVisionRadar != null)
                    mapScreen.targetedPlayer.nightVisionRadar.enabled = true;
            }
        }

        public static void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            ResetRendering();
        }

        static void ResetRendering()
        {
            if (!armsNotRendering.HasValue)
                return;

            if (GlobalReferences.viewmodelArms != null)
            {
                GlobalReferences.viewmodelArms.forceRenderingOff = (bool)armsNotRendering;
                armsNotRendering = null;
            }

            PlayerControllerB targetedPlayer = StartOfRound.Instance.mapScreen?.targetedPlayer;
            if (targetedPlayer != null)
            {
                if (targetedPlayer.nightVisionRadar != null)
                    targetedPlayer.nightVisionRadar.enabled = false;

                if (targetedPlayer.deadBody?.nightVisionRadar != null)
                    targetedPlayer.deadBody.nightVisionRadar.enabled = false;
            }
        }
    }
}
