using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;

namespace ButteryFixes.Utility
{
    internal static class CruiserAnimator
    {
        static Dictionary<PlayerControllerB, Transform> keyHolders = [];

        // thanks to Scandal for all of these coordinates
        static readonly Vector3 serverHolderPos = new(-0.04170258f, 6.530248e-05f, -0.03752365f);
        static readonly Quaternion serverHolderRot = Quaternion.Euler(13.794f, -3.466f, -159.053f);

        static readonly Quaternion armsMetarigParentRot = Quaternion.Euler(90f, 0f, 0f);
        static readonly Quaternion armsMetarigRot = Quaternion.Euler(-90f, 0f, 0f);

        static readonly Vector3 localArmsPos = new(0, -0.008f, -0.43f);
        static readonly Quaternion localArmsRot = Quaternion.Euler(84.78056f, 0f, 0f);

        static readonly Quaternion playerBodyRot = Quaternion.Euler(-90f, 0f, 0f);

        internal static void AnimateKeyForCruiser(VehicleController cruiser)
        {
            if (!keyHolders.TryGetValue(cruiser.currentDriver, out Transform keyHolder) && GameNetworkManager.Instance != null && !GameNetworkManager.Instance.isDisconnecting)
            {
                if (cruiser.localPlayerInControl)
                {
                    if (cruiser.currentDriver.localItemHolder != null)
                    {
                        keyHolder = new GameObject("ButteryFixes_LocalCarKeyHolder").transform;
                        keyHolder.SetParent(cruiser.currentDriver.localItemHolder.parent, false);
                        keyHolder.SetLocalPositionAndRotation(new(-0.002f, 0.036f, -0.042f), Quaternion.Euler(-3.616f, -2.302f, -179.855f));
                    }
                }
                else if (cruiser.currentDriver.serverItemHolder != null)
                {
                    keyHolder = new GameObject("ButteryFixes_ServerCarKeyHolder").transform;
                    keyHolder.SetParent(cruiser.currentDriver.serverItemHolder.parent, false);
                    keyHolder.SetLocalPositionAndRotation(serverHolderPos, serverHolderRot);
                }

                if (keyHolder != null)
                {
                    keyHolder.position += keyHolder.rotation * new Vector3(-cruiser.positionOffset.x, -cruiser.positionOffset.y, cruiser.positionOffset.z);
                    keyHolders.Add(cruiser.currentDriver, keyHolder);
                }
            }

            if (keyHolder != null)
                cruiser.keyObject.transform.SetPositionAndRotation(keyHolder.position, keyHolder.rotation);
        }

        internal static void Reset()
        {
            keyHolders.Clear();
        }

        internal static void ResetPlayerAnimator(PlayerControllerB player)
        {
            player.playerModelArmsMetarig.parent.transform.localRotation = armsMetarigParentRot;
            player.playerModelArmsMetarig.localRotation = armsMetarigRot;
            player.localArmsTransform.SetLocalPositionAndRotation(localArmsPos, localArmsRot);
            player.playerBodyAnimator.transform.SetLocalPositionAndRotation(Vector3.zero, playerBodyRot);
        }
    }
}
