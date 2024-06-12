using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ButteryFixes.Utility
{
    static class NonPatchFunctions
    {
        public static void ShotgunPreProcess(Vector3 shotgunPosition, ref int num, ref RaycastHit[] results)
        {
            int index = 0;
            List<EnemyAI> enemies = new();

            // sort in order of distance
            RaycastHit[] sortedResults = results.Take(num).OrderBy(hit => Vector3.Distance(shotgunPosition, hit.point)).ToArray();

            // remove all duplicates
            for (int i = 0; i < num; i++)
            {
                if (sortedResults[i].transform.TryGetComponent(out EnemyAICollisionDetect enemyCollider) && !enemyCollider.onlyCollideWhenGrounded && !enemies.Contains(enemyCollider.mainScript))
                {
                    enemies.Add(enemyCollider.mainScript);
                    if (!enemyCollider.mainScript.isEnemyDead)
                    {
                        results[index] = sortedResults[i];
                        index++;
                        // only hit 10 targets max
                        if (index == 10)
                            break;
                    }
                }
            }

            num = index;
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
            // the below doesn't work as expected because jumping cancels the sprint animation
            bool sprinting = player.IsOwner ? player.isSprinting : player.playerBodyAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Sprinting");
            Plugin.Logger.LogDebug($"Bunnyhop while sprinting: {sprinting}");
            if (sprinting)
                RoundManager.Instance.PlayAudibleNoise(player.transform.position, 22f, 0.6f, 0, noiseIsInsideClosedShip, 3322);
            else
                RoundManager.Instance.PlayAudibleNoise(player.transform.position, 17f, 0.4f, 0, noiseIsInsideClosedShip, 3322);
        }
    }
}
