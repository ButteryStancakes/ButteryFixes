using UnityEngine.Rendering;
using UnityEngine;

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
        }
    }
}
