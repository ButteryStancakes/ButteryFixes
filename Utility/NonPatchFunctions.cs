using GameNetcodeStuff;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace ButteryFixes.Utility
{
    public static class NonPatchFunctions
    {
        internal static bool[] playerWasLastSprinting = new bool[/*4*/50];
        internal static int hives;

        internal static IEnumerator ShellsAppearAfterDelay(ShotgunItem shotgun)
        {
            bool wasHeldByEnemy = shotgun.isHeldByEnemy;
            float timeSinceStart = Time.realtimeSinceStartup;

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

            if (!wasHeldByEnemy)
                yield return new WaitUntil(() => !shotgun.isReloading || Time.realtimeSinceStartup - timeSinceStart > 3f);

            // disables shells rendering when the gun is closed, to prevent bleedthrough with LOD1 model
            shotgun.shotgunShellLeft.forceRenderingOff = true;
            shotgun.shotgunShellRight.forceRenderingOff = true;
            Plugin.Logger.LogDebug($"Finished animating shotgun shells (held by enemy: {shotgun.isHeldByEnemy})");
        }

        internal static void FakeFootstepAlert(PlayerControllerB player)
        {
            bool noiseIsInsideClosedShip = player.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed;
            if ((int)player.actualClientId < playerWasLastSprinting.Length && (player.IsOwner ? player.isSprinting : playerWasLastSprinting[(int)player.actualClientId]))
                RoundManager.Instance.PlayAudibleNoise(player.transform.position, 22f, 0.6f, 0, noiseIsInsideClosedShip, 3322);
            else
                RoundManager.Instance.PlayAudibleNoise(player.transform.position, 17f, 0.4f, 0, noiseIsInsideClosedShip, 3322);
        }

        internal static void ForceRefreshAllHelmetLights(PlayerControllerB player, bool forceOff = false)
        {
            for (int i = 0; i < player.allHelmetLights.Length; i++)
            {
                bool enable = false;
                if (!forceOff)
                {
                    if (player.ItemOnlySlot != null)
                    {
                        FlashlightItem flashlightItemUtility = player.ItemOnlySlot as FlashlightItem;
                        if (flashlightItemUtility != null && flashlightItemUtility.flashlightTypeID == i && flashlightItemUtility.isPocketed && flashlightItemUtility.isBeingUsed && flashlightItemUtility.insertedBattery.charge > 0f)
                            enable = true;
                    }

                    if (!enable)
                    {
                        for (int j = 0; j < player.ItemSlots.Length; j++)
                        {
                            if (player.ItemSlots[j] != null)
                            {
                                FlashlightItem flashlightItem = player.ItemSlots[j] as FlashlightItem;
                                if (flashlightItem != null && flashlightItem.flashlightTypeID == i && flashlightItem.isPocketed && flashlightItem.isBeingUsed && flashlightItem.insertedBattery.charge > 0f)
                                {
                                    enable = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (player.allHelmetLights[i].enabled != enable)
                {
                    player.allHelmetLights[i].enabled = enable;
                    Plugin.Logger.LogDebug($"Fixed erroneous active state of {player.playerUsername}'s helmet light \"{player.allHelmetLights[i].name}\" (now {enable})");
                }
            }
        }

        internal static void SmokingHotCorpse(Transform body)
        {
            if (GlobalReferences.smokeParticle == null || !body.TryGetComponent(out SkinnedMeshRenderer mesh))
                return;

            foreach (Transform child in body)
                if (child.name.StartsWith(GlobalReferences.smokeParticle.name))
                    return;

            GameObject smokeParticle = Object.Instantiate(GlobalReferences.smokeParticle, body.transform);
            smokeParticle.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            smokeParticle.transform.localScale = Vector3.one;
            ParticleSystem.ShapeModule shape = smokeParticle.GetComponent<ParticleSystem>().shape;
            shape.skinnedMeshRenderer = mesh;
            Plugin.Logger.LogDebug("Smoke from freshly burnt corpse");
        }

        public static int RerollHivePrice(int price, Vector3 pos)
        {
            hives++;

            if (!Configuration.fixSurfacePrices.Value || StartOfRound.Instance.isChallengeFile)
                return price;

            System.Random random = new(StartOfRound.Instance.randomMapSeed + 1314 + hives);

            int temp = random.Next(50, 150);
            if (Vector3.Distance(pos, StartOfRound.Instance.shipLandingPosition.position) < 40f)
                temp = random.Next(40, 100);

            Plugin.Logger.LogDebug($"Hive price recalculated: ${price} -> ${temp}");
            return temp;
        }

        internal static IEnumerator InteractionTemporarilyLocksCamera(InteractTrigger trigger)
        {
            if (trigger == null)
                yield break;

            float startTime = Time.realtimeSinceStartup;

            while (trigger.playerScriptInSpecialAnimation != GameNetworkManager.Instance.localPlayerController && Time.realtimeSinceStartup - startTime < 2f)
                yield return null;

            if (trigger.playerScriptInSpecialAnimation != GameNetworkManager.Instance.localPlayerController)
            {
                Plugin.Logger.LogDebug("Failed to lock animation for local player, who never entered animation state");
                yield break;
            }

            GlobalReferences.lockingCamera++;

            yield return new WaitForSeconds(trigger.animationWaitTime);

            if (GlobalReferences.lockingCamera > 0)
                GlobalReferences.lockingCamera--;
        }

        public static Vector3 GetTrueExitPoint(bool ignore = true, bool ignore2 = false)
        {
            // in mineshaft, point to the bottom of the elevator instead
            if (RoundManager.Instance.currentDungeonType == 4 && RoundManager.Instance.currentMineshaftElevator != null && !GlobalReferences.mineStartBounds.Contains(StartOfRound.Instance.mapScreen.mapCamera.transform.position - (Vector3.up * 3.75f)))
                return RoundManager.Instance.currentMineshaftElevator.elevatorBottomPoint.position;

            return GlobalReferences.mainEntrancePos;
        }

        internal static void ClearMicrowave()
        {
            for (int i = 0; i < GlobalReferences.microwavedItems.Count; i++)
            {
                if (GlobalReferences.microwavedItems[i] == null)
                    continue;

                GlobalReferences.microwavedItems[i].rotateObject = false;
            }

            GlobalReferences.microwavedItems.Clear();
        }

        public static PlayerControllerB CruiserCreditsPlayer()
        {
            if (GlobalReferences.lastDriver != null)
                return GlobalReferences.lastDriver;

            return GameNetworkManager.Instance.localPlayerController;
        }

        internal static IEnumerator LightningShakesBody(DeadBodyInfo deadBodyInfo)
        {
            yield return new WaitForFixedUpdate();
            if (Time.realtimeSinceStartup - GlobalReferences.lightningLastStruck <= 2f && deadBodyInfo?.playerScript != null && Vector3.Distance(GlobalReferences.lastLightningStrike, deadBodyInfo.playerScript.positionOfDeath) <= 7f)
            {
                Plugin.Logger.LogDebug($"Player \"{deadBodyInfo.playerScript.playerUsername}\" was likely struck by lightning, shake rigidbodies");
                ShakeRigidbodies shakeRigidbodies = deadBodyInfo.gameObject.AddComponent<ShakeRigidbodies>();
                shakeRigidbodies.rigidBodies = [.. deadBodyInfo.GetComponentsInChildren<Rigidbody>().Where(rb => rb.gameObject.layer == 20 && !rb.name.EndsWith("lower") && !rb.name.StartsWith("shin"))];
                shakeRigidbodies.shakeTimer = 5f;
                shakeRigidbodies.shakeIntensity = 4755f;
            }
        }

        internal static IEnumerator TryAutoCollectBody(DeadBodyInfo deadBodyInfo)
        {
            float timeOfDeath = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - timeOfDeath <= 2f && deadBodyInfo?.grabBodyObject == null)
                yield return null;

            if (deadBodyInfo != null && !deadBodyInfo.deactivated && deadBodyInfo.grabBodyObject != null && !deadBodyInfo.grabBodyObject.deactivated && deadBodyInfo.playerScript != null)
            {
                deadBodyInfo.playerScript.SetItemInElevator(true, true, deadBodyInfo.grabBodyObject);
                RoundManager.Instance.CollectNewScrapForThisRound(deadBodyInfo.grabBodyObject);
                Plugin.Logger.LogDebug($"Player \"{deadBodyInfo.playerScript?.playerUsername}\" automatically collected themself");
            }
        }
    }
}
