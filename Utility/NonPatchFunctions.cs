using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ButteryFixes.Utility
{
    static class NonPatchFunctions
    {
        public static void ShotgunPreProcess(ref int num, ref RaycastHit[] results)
        {
            int index = 0;
            List<EnemyAI> enemies = new();

            // remove all duplicates
            for (int i = 0; i < num; i++)
            {
                if (results[i].transform.TryGetComponent(out EnemyAICollisionDetect enemyCollider))
                {
                    if (enemyCollider.onlyCollideWhenGrounded)
                        Plugin.Logger.LogInfo($"Filtered shotgun hit on \"{enemyCollider.mainScript.name}\" (wouldn't deal damage)");
                    else if (!enemies.Contains(enemyCollider.mainScript))
                    {
                        enemies.Add(enemyCollider.mainScript);
                        if (enemyCollider.mainScript.isEnemyDead)
                            Plugin.Logger.LogInfo($"Filtered shotgun hit on \"{enemyCollider.mainScript.name}\" (already dead)");
                        else
                        {
                            if (i > index)
                                results[index] = results[i];
                            index++;
                            if (i >= 10)
                                Plugin.Logger.LogInfo($"Registered shotgun hit on \"{enemyCollider.mainScript.name}\" (index {i} > 9; vanilla would have skipped)");
                            if (index == 11)
                                Plugin.Logger.LogInfo("Registered 11th target; will have to sort and skip some");
                        }
                    }
                    else
                        Plugin.Logger.LogInfo($"Filtered shotgun hit on \"{enemyCollider.mainScript.name}\" (duplicate)");
                }
            }

            // if hit more than 10 targets, filter out all but the closest 10
            if (index > 10)
            {
                List<RaycastHit> resultsList = results.Take(index).OrderBy(hit => hit.distance).ToList();
                for (int i = 0; i < resultsList.Count; i++)
                {
                    if (i < 10)
                        results[i] = resultsList[i];
                    else
                        Plugin.Logger.LogInfo($"Skipped shotgun hit on \"{resultsList[i].transform.GetComponent<EnemyAICollisionDetect>().mainScript.name}\" (index {i} > 9)");
                }
            }

            num = Mathf.Min(10, index);
        }

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
            bool sprinting = player.IsOwner ? player.isSprinting : player.playerBodyAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Sprinting");
            Plugin.Logger.LogDebug($"Bunnyhop while sprinting: {sprinting}");
            if (sprinting)
                RoundManager.Instance.PlayAudibleNoise(player.transform.position, 22f, 0.6f, 0, noiseIsInsideClosedShip, 3322);
            else
                RoundManager.Instance.PlayAudibleNoise(player.transform.position, 17f, 0.4f, 0, noiseIsInsideClosedShip, 3322);
        }
    }
}
