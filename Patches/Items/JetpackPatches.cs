using ButteryFixes.Utility;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Items
{
    internal class JetpackPatches
    {
        [HarmonyPatch(typeof(JetpackItem), "DeactivateJetpack")]
        [HarmonyPostfix]
        public static void PostDeactivateJetpack(JetpackItem __instance)
        {
            EnemyType flowerSnake = GlobalReferences.allEnemiesList["FlowerSnake"];

            // if the jetpack turns off and there are tulip snakes on the map...
            if (flowerSnake != null && flowerSnake.numberSpawned > 0)
            {
                foreach (EnemyAI enemyAI in RoundManager.Instance.SpawnedEnemies)
                {
                    if (enemyAI is FlowerSnakeEnemy)
                    {
                        FlowerSnakeEnemy tulipSnake = enemyAI as FlowerSnakeEnemy;
                        // verify there is a living tulip snake clung to the player and flapping its wings
                        if (!tulipSnake.isEnemyDead && tulipSnake.clingingToPlayer == GameNetworkManager.Instance.localPlayerController && tulipSnake.clingingToPlayer.disablingJetpackControls && tulipSnake.clingPosition == 4 && tulipSnake.flightPower > 0f && (PlayerControllerB)ReflectionCache.JETPACK_ITEM_PREVIOUS_PLAYER_HELD_BY.GetValue(__instance) == tulipSnake.clingingToPlayer)
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

        [HarmonyPatch(typeof(JetpackItem), nameof(JetpackItem.ExplodeJetpackClientRpc))]
        [HarmonyPostfix]
        public static void PostExplodeJetpackClientRpc(JetpackItem __instance, PlayerControllerB ___previousPlayerHeldBy)
        {
            if (__instance.IsOwner || Plugin.DISABLE_PLAYERMODEL_PATCHES)
                return;

            foreach (DeadBodyInfo deadBodyInfo in Object.FindObjectsOfType<DeadBodyInfo>())
            {
                if (deadBodyInfo.playerScript == ___previousPlayerHeldBy)
                {
                    foreach (Renderer rend in deadBodyInfo.GetComponentsInChildren<Renderer>())
                    {
                        if (rend.gameObject.layer == 0 && (rend.name.StartsWith("BetaBadge") || rend.name.StartsWith("LevelSticker")))
                            rend.forceRenderingOff = true;
                        else if (rend.gameObject.layer == 20)
                            rend.sharedMaterial = GlobalReferences.scavengerSuitBurnt;
                    }

                    Plugin.Logger.LogInfo("Jetpack exploded and burned player corpse");
                    return;
                }
            }

            Plugin.Logger.LogWarning("Jetpack exploded but the player that crashed it didn't spawn a body");
        }
    }
}
