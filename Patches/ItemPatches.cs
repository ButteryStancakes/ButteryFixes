using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using UnityEngine;
using ButteryFixes.Utility;

namespace ButteryFixes.Patches
{
    [HarmonyPatch]
    internal class ItemPatches
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
                __instance.StartCoroutine(NonPatchFunctions.ShellsAppearAfterDelay(__instance));
                Plugin.Logger.LogInfo("Shotgun was reloaded by another client; animating shells");
            }
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

        [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ShootGun))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ShotgunItemTransShootGun(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = instructions.ToList();

            bool fixEarsRinging = false;
            for (int i = 2; i < codes.Count; i++)
            {
                // first distance check for tinnitus/screenshake
                if (!fixEarsRinging && codes[i].opcode == OpCodes.Bge_Un && codes[i - 2].opcode == OpCodes.Ldloc_2)
                {
                    for (int j = i + 1; j < codes.Count - 1; j++)
                    {
                        int insertAt = -1;
                        if (codes[j + 1].opcode == OpCodes.Ldloc_2)
                        {
                            // first jump from if/else branches
                            if (insertAt >= 0 && codes[j].opcode == OpCodes.Br)
                            {
                                codes.Insert(insertAt, new CodeInstruction(OpCodes.Br, codes[j].operand));
                                Plugin.Logger.LogDebug("Transpiler: Fix ear-ringing severity in extremely close range");
                                fixEarsRinging = true;
                                break;
                            }
                            // the end of the first if branch
                            else if (insertAt < 0 && codes[j].opcode == OpCodes.Stloc_S)
                                insertAt = j + 1;
                        }
                    }
                }
                else if (codes[i].opcode == OpCodes.Newarr && (System.Type)codes[i].operand == typeof(RaycastHit) && codes[i - 1].opcode == OpCodes.Ldc_I4_S && (sbyte)codes[i - 1].operand == 10)
                {
                    codes[i - 1].operand = 50;
                    Plugin.Logger.LogDebug("Transpiler: Resize shotgun collider array");
                }
            }

            return codes;
        }

        [HarmonyPatch(typeof(LungProp), nameof(LungProp.Start))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        static void LungPropPostStart(LungProp __instance)
        {
            ScanNodeProperties scanNodeProperties = __instance.GetComponentInChildren<ScanNodeProperties>();
            if (scanNodeProperties != null)
            {
                if (scanNodeProperties.headerText == "Apparatice")
                    scanNodeProperties.headerText = "Apparatus";
                scanNodeProperties.subText = $"Value: ${__instance.scrapValue}";
                Plugin.Logger.LogInfo("Scan node: Apparatus");
            }
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.GetNewStoryLogClientRpc))]
        [HarmonyPostfix]
        static void PostGetNewStoryLogClientRpc(int logID)
        {
            foreach (StoryLog storyLog in Object.FindObjectsOfType<StoryLog>())
            {
                if (storyLog.storyLogID == logID)
                {
                    storyLog.CollectLog();
                    Plugin.Logger.LogInfo($"Another player collected data chip #{logID}");
                    return;
                }
            }
        }

        [HarmonyPatch(typeof(HauntedMaskItem), nameof(HauntedMaskItem.MaskClampToHeadAnimationEvent))]
        [HarmonyPostfix]
        static void PostMaskClampToHeadAnimationEvent(HauntedMaskItem __instance)
        {
            if (__instance.maskTypeId == 5)
            {
                Plugin.Logger.LogInfo("Player is being converted by a Tragedy mask; about to replace mask prefab appearance");
                NonPatchFunctions.ConvertMaskToTragedy(__instance.currentHeadMask.transform);
            }
        }

        [HarmonyPatch(typeof(JetpackItem), nameof(JetpackItem.ExplodeJetpackClientRpc))]
        [HarmonyPostfix]
        public static void PostExplodeJetpackClientRpc(JetpackItem __instance, PlayerControllerB ___previousPlayerHeldBy)
        {
            if (Plugin.DISABLE_PLAYERMODEL_PATCHES)
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
                            rend.material = GlobalReferences.scavengerSuitBurnt;
                    }

                    Plugin.Logger.LogInfo("Jetpack exploded and burned player corpse");
                }
            }

            Plugin.Logger.LogWarning("Jetpack exploded but the player that crashed it didn't spawn a body");
        }
    }
}
