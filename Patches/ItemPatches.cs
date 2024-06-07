using GameNetcodeStuff;
using HarmonyLib;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace ButteryFixes.Patches
{
    [HarmonyPatch]
    internal class ItemPatches
    {
        internal static readonly FieldInfo JETPACK_ACTIVATED = typeof(JetpackItem).GetField("jetpackActivated", BindingFlags.Instance | BindingFlags.NonPublic);

        static readonly FieldInfo PREVIOUS_PLAYER_HELD_BY = typeof(JetpackItem).GetField("previousPlayerHeldBy", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPatch(typeof(JetpackItem), "DeactivateJetpack")]
        [HarmonyPostfix]
        public static void PostDeactivateJetpack(JetpackItem __instance)
        {
            // if the jetpack turns off and there are tulip snakes on the map...
            if (EnemyPatches.FLOWER_SNAKE != null && EnemyPatches.FLOWER_SNAKE.numberSpawned > 0)
            {
                foreach (EnemyAI enemyAI in RoundManager.Instance.SpawnedEnemies)
                {
                    if (enemyAI is FlowerSnakeEnemy)
                    {
                        FlowerSnakeEnemy tulipSnake = enemyAI as FlowerSnakeEnemy;
                        // verify there is a living tulip snake clung to the player and flapping its wings
                        if (!tulipSnake.isEnemyDead && tulipSnake.clingingToPlayer == GameNetworkManager.Instance.localPlayerController && tulipSnake.clingingToPlayer.disablingJetpackControls && tulipSnake.clingPosition == 4 && tulipSnake.flightPower > 0f && (PlayerControllerB)PREVIOUS_PLAYER_HELD_BY.GetValue(__instance) == tulipSnake.clingingToPlayer)
                        {
                            tulipSnake.clingingToPlayer.disablingJetpackControls = false;
                            // can't set maxJetpackAngle after player has been flying with free rotation (causes lockup and generally feels bad)
                            // however, jetpackRandomIntensity is capped by maxJetpackAngle (so must make arbitrarily high, rather than -1)
                            // jetpackRandomIntensity of 60 should be somewhat similar to normal tulip snake interference (same max, slightly less intense on average)
                            tulipSnake.clingingToPlayer.maxJetpackAngle = float.MaxValue; //60f;
                            tulipSnake.clingingToPlayer.jetpackRandomIntensity = 60f; //120f;
                            Plugin.Logger.LogInfo("Jetpack disabled, but tulip snake is still carrying");
                            return;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.ChargeBatteries))]
        [HarmonyPostfix]
        static void PostChargeBatteries(GrabbableObject __instance)
        {
            if (__instance is BoomboxItem)
            {
                BoomboxItem boomboxItem = __instance as BoomboxItem;
                // needs to verify charge is > 0 because there's a special pitch effect on battery death we don't want to interrupt
                if (boomboxItem.isPlayingMusic && boomboxItem.boomboxAudio.pitch < 1f && boomboxItem.insertedBattery.charge > 0f)
                {
                    boomboxItem.boomboxAudio.pitch = 1f;
                    Plugin.Logger.LogInfo("Boombox was recharged, correcting pitch");
                }
            }
        }

        [HarmonyPatch(typeof(JetpackItem), nameof(JetpackItem.EquipItem))]
        [HarmonyPostfix]
        static void JetpackItemPostEquipItem(JetpackItem __instance)
        {
            // Doppler effect is only meant to apply to audio waves travelling towards or away from the listener (not a jetpack strapped to your back)
            if (__instance.playerHeldBy == GameNetworkManager.Instance.localPlayerController)
            {
                __instance.jetpackAudio.dopplerLevel = 0f;
                __instance.jetpackBeepsAudio.dopplerLevel = 0f;
                Plugin.Logger.LogInfo("Jetpack held by you, disable doppler effect");
            }
            else
            {
                __instance.jetpackAudio.dopplerLevel = 1f;
                __instance.jetpackBeepsAudio.dopplerLevel = 1f;
                Plugin.Logger.LogInfo("Jetpack held by other player, enable doppler effect");
            }
        }

        [HarmonyPatch(typeof(JetpackItem), nameof(JetpackItem.Update))]
        [HarmonyPostfix]
        static void JetpackItemPostUpdate(bool ___jetpackActivated, PlayerControllerB ___previousPlayerHeldBy)
        {
            if (___jetpackActivated)
            {
                // regain full directional movement when activating jetpack after tulip snake takeoff
                if (___previousPlayerHeldBy.maxJetpackAngle >= 0f && ___previousPlayerHeldBy.maxJetpackAngle < 360f)
                {
                    ___previousPlayerHeldBy.maxJetpackAngle = float.MaxValue; //-1f;
                    ___previousPlayerHeldBy.jetpackRandomIntensity = 60f; //0f;
                    Plugin.Logger.LogInfo("Uncap player rotation (using jetpack while tulip snakes riding)");
                }
            }
        }

        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ReloadGunEffectsClientRpc))]
        [HarmonyPostfix]
        static void PostReloadGunEffectsClientRpc(ShotgunItem __instance, bool start)
        {
            // controls shells appearing/disappearing during reload for all clients (except for the one holding the gun)
            if (start && !__instance.IsOwner)
            {
                __instance.shotgunShellLeft.enabled = __instance.shellsLoaded > 0;
                __instance.shotgunShellRight.enabled = false;
                __instance.StartCoroutine(ShellsAppearAfterDelay(__instance));
                Plugin.Logger.LogInfo("Shotgun was reloaded by another client; animating shells");
            }
        }

        internal static IEnumerator ShellsAppearAfterDelay(ShotgunItem shotgun)
        {
            yield return new WaitForSeconds(shotgun.isHeldByEnemy ? 0.85f : 1.9f);

            if (shotgun.isHeldByEnemy)
            {
                // nutcrackers don't set isReloading (see below)
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

        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.Update))]
        [HarmonyPostfix]
        static void ShotgunItemPostUpdate(ShotgunItem __instance)
        {
            // shells should render during the reload animation (this specific patch only works for players)
            if (__instance.isReloading)
            {
                __instance.shotgunShellLeft.forceRenderingOff = false;
                __instance.shotgunShellRight.forceRenderingOff = false;
            }
        }

        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.Start))]
        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.DiscardItem))]
        [HarmonyPostfix]
        static void DontRenderShotgunShells(ShotgunItem __instance)
        {
            __instance.shotgunShellLeft.forceRenderingOff = true;
            __instance.shotgunShellRight.forceRenderingOff = true;
        }
    }
}
