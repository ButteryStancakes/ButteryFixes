using GameNetcodeStuff;
using HarmonyLib;
using System.Reflection;

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
            if (EnemyPatches.FLOWER_SNAKE != null && EnemyPatches.FLOWER_SNAKE.numberSpawned > 0)
            {
                foreach (EnemyAI enemyAI in RoundManager.Instance.SpawnedEnemies)
                {
                    if (enemyAI is FlowerSnakeEnemy)
                    {
                        FlowerSnakeEnemy tulipSnake = enemyAI as FlowerSnakeEnemy;
                        if (!tulipSnake.isEnemyDead && tulipSnake.clingingToPlayer == GameNetworkManager.Instance.localPlayerController && tulipSnake.clingingToPlayer.disablingJetpackControls && tulipSnake.clingPosition == 4 && tulipSnake.flightPower > 0f && (PlayerControllerB)PREVIOUS_PLAYER_HELD_BY.GetValue(__instance) == tulipSnake.clingingToPlayer)
                        {
                            tulipSnake.clingingToPlayer.disablingJetpackControls = false;
                            tulipSnake.clingingToPlayer.maxJetpackAngle = 360f; //60f;
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
                if (boomboxItem.isPlayingMusic && boomboxItem.boomboxAudio.pitch < 1f && (boomboxItem.boomboxAudio.pitch >= 0.8f || boomboxItem.boomboxAudio.pitch <= 0f))
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
                if (___previousPlayerHeldBy.maxJetpackAngle >= 0f)
                {
                    ___previousPlayerHeldBy.maxJetpackAngle = 360f; //-1f;
                    ___previousPlayerHeldBy.jetpackRandomIntensity = 60f; //0f;
                    Plugin.Logger.LogInfo("Uncap player rotation (using jetpack while tulip snakes riding)");
                }
            }
        }
    }
}
