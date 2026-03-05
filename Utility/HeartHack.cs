using UnityEngine;

namespace ButteryFixes.Utility
{
    internal class HeartHack : MonoBehaviour
    {
        internal LoopShapeKey loopShapeKey;

        float time;

        void LateUpdate()
        {
            if (loopShapeKey != null)
            {
                // need to keep a custom timer, that we can increment a variable amount per frame
                time += Time.deltaTime * loopShapeKey.fearMultiplier;

                if (loopShapeKey.skinnedMeshRenderer != null)
                {
                    // overwrite original values
                    loopShapeKey.skinnedMeshRenderer.SetBlendShapeWeight(0, Mathf.PingPong(time * 156f, 100f)); // 156 = 260 * 0.6 (from LoopShapeKey)
                    loopShapeKey.skinnedMeshRenderer.SetBlendShapeWeight(1, Mathf.PingPong(time * 120f, 100f)); // 120 = 200 * 0.6 (from LoopShapeKey)
                }
            }
            else
            {
                loopShapeKey = GetComponent<LoopShapeKey>();

                if (loopShapeKey == null)
                {
                    Plugin.Logger.LogWarning($"Error initializing heartbeat animation for {name}");
                    enabled = false;
                    return;
                }
            }
        }
    }
}
