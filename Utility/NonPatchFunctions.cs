using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ButteryFixes.Utility
{
    static class NonPatchFunctions
    {
        internal static bool[] playerWasLastSprinting = new bool[/*4*/50];

        internal static IEnumerator ShellsAppearAfterDelay(ShotgunItem shotgun)
        {
            yield return new WaitForSeconds(shotgun.isHeldByEnemy ? 0.85f : 1.9f);

            if (shotgun.isHeldByEnemy)
            {
                // nutcrackers don't set isReloading
                shotgun.shotgunShellLeft.forceRenderingOff = false;
                shotgun.shotgunShellLeft.enabled = true;
                shotgun.shotgunShellRight.forceRenderingOff = false;
                shotgun.shotgunShellRight.enabled = true;
            }
            else if (shotgun.shotgunShellLeft.enabled)
                shotgun.shotgunShellRight.enabled = true;
            else
                shotgun.shotgunShellLeft.enabled = true;

            yield return new WaitForSeconds(shotgun.isHeldByEnemy ? 0.66f : 0.75f);

            if (!shotgun.isHeldByEnemy)
                yield return new WaitUntil(() => !shotgun.isReloading);

            // disables shells rendering when the gun is closed, to prevent bleedthrough with LOD1 model
            shotgun.shotgunShellLeft.forceRenderingOff = true;
            shotgun.shotgunShellRight.forceRenderingOff = true;
            Plugin.Logger.LogInfo($"Finished animating shotgun shells (held by enemy: {shotgun.isHeldByEnemy})");
        }

        internal static void FakeFootstepAlert(PlayerControllerB player)
        {
            bool noiseIsInsideClosedShip = player.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed;
            if (player.IsOwner ? player.isSprinting : playerWasLastSprinting[player.actualClientId])
                RoundManager.Instance.PlayAudibleNoise(player.transform.position, 22f, 0.6f, 0, noiseIsInsideClosedShip, 3322);
            else
                RoundManager.Instance.PlayAudibleNoise(player.transform.position, 17f, 0.4f, 0, noiseIsInsideClosedShip, 3322);
        }

        internal static void ConvertMaskToTragedy(Transform mask)
        {
            Transform mesh = mask.Find("Mesh");
            if (mesh != null && GlobalReferences.tragedyMask != null && GlobalReferences.tragedyMaskMat != null)
            {
                mesh.GetComponent<MeshFilter>().mesh = GlobalReferences.tragedyMask;
                mesh.GetComponent<MeshRenderer>().sharedMaterial = GlobalReferences.tragedyMaskMat;

                MeshFilter tragedyMaskEyesFilled = mesh.Find("EyesFilled")?.GetComponent<MeshFilter>();
                if (tragedyMaskEyesFilled != null && GlobalReferences.tragedyMaskEyesFilled != null)
                {
                    tragedyMaskEyesFilled.mesh = GlobalReferences.tragedyMaskEyesFilled;

                    MeshFilter tragedyMaskLOD = mask.Find("ComedyMaskLOD1")?.GetComponent<MeshFilter>();
                    if (tragedyMaskLOD != null && GlobalReferences.tragedyMaskLOD != null)
                    {
                        tragedyMaskLOD.mesh = GlobalReferences.tragedyMaskLOD;
                        tragedyMaskLOD.GetComponent<MeshRenderer>().sharedMaterial = GlobalReferences.tragedyMaskMat;

                        Plugin.Logger.LogInfo("All mask attachment meshes replaced successfully");
                    }
                    else
                        Plugin.Logger.LogWarning("Failed to replace mask attachment eyes");
                }
                else
                    Plugin.Logger.LogWarning("Failed to replace mask attachment LOD");
            }
            else
                Plugin.Logger.LogWarning("Failed to replace mask attachment mesh");
        }
    }
}
