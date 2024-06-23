using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using GameNetcodeStuff;
using ButteryFixes.Utility;
using UnityEngine.Rendering.HighDefinition;

namespace ButteryFixes.Patches
{
    [HarmonyPatch]
    internal class EnemyPatches
    {
        [HarmonyPatch(typeof(ButlerEnemyAI), nameof(ButlerEnemyAI.OnCollideWithPlayer))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ButlerEnemyAITransOnCollideWithPlayer(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo timeSinceStealthStab = typeof(ButlerEnemyAI).GetField("timeSinceStealthStab", BindingFlags.Instance | BindingFlags.NonPublic);
            int startAt = -1, endAt = -1;
            for (int i = 0; i < codes.Count; i++)
            {
                if (startAt >= 0 && endAt >= 0)
                {
                    for (int j = startAt; j <= endAt; j++)
                    {
                        codes[j].opcode = OpCodes.Nop;
                        //codes[j].operand = null;
                    }
                    Plugin.Logger.LogDebug("Transpiler: Remove timestamp check (replace with prefix)");
                    return codes;
                }
                else if (startAt == -1)
                {
                    if (codes[i].opcode == OpCodes.Stfld && (FieldInfo)codes[i].operand == timeSinceStealthStab)
                        startAt = i - 2;
                }
                else if (i > startAt && codes[i].opcode == OpCodes.Ret)
                    endAt = i;
            }

            Plugin.Logger.LogError("Butler collision transpiler failed");
            return codes;
        }

        [HarmonyPatch(typeof(ButlerEnemyAI), nameof(ButlerEnemyAI.OnCollideWithPlayer))]
        [HarmonyPrefix]
        static bool ButlerEnemyAIPreOnCollideWithPlayer(ButlerEnemyAI __instance, ref float ___timeSinceStealthStab)
        {
            // recreate the timestamp check since we need to run some additional logic
            if (!__instance.isEnemyDead && __instance.currentBehaviourStateIndex != 2)
            {
                if (Time.realtimeSinceStartup - ___timeSinceStealthStab < 10f)
                    return false;

                Plugin.Logger.LogInfo("Butler rolls chance for \"stealth stab\"");
                if (Random.Range(0, 100) < 86)
                {
                    ___timeSinceStealthStab = Time.realtimeSinceStartup;
                    Plugin.Logger.LogInfo("Stealth stab chance failed (won't check for 10s)");
                    return false;
                }
                Plugin.Logger.LogInfo("Stealth stab chance succeeds (aggro for 3s)");
            }

            return true;
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.EnableEnemyMesh))]
        [HarmonyPrefix]
        static void EnemyAIPreEnableEnemyMesh(EnemyAI __instance)
        {
            // minor optimization; this bug only really affects mimics
            if (__instance is not MaskedPlayerEnemy)
                return;

            if (__instance.skinnedMeshRenderers.Length > 0)
            {
                for (int i = 0; i < __instance.skinnedMeshRenderers.Length; i++)
                {
                    if (__instance.skinnedMeshRenderers == null)
                    {
                        __instance.skinnedMeshRenderers = __instance.skinnedMeshRenderers.Where(skinnedMeshRenderer => skinnedMeshRenderer != null).ToArray();
                        Plugin.Logger.LogWarning($"Removed all missing Skinned Mesh Renderers from enemy \"{__instance.name}\"");
                        break;
                    }
                }
            }
            if (__instance.meshRenderers.Length > 0)
            {
                for (int i = 0; i < __instance.meshRenderers.Length; i++)
                {
                    if (__instance.meshRenderers == null)
                    {
                        __instance.meshRenderers = __instance.meshRenderers.Where(meshRenderer => meshRenderer != null).ToArray();
                        Plugin.Logger.LogWarning($"Removed all missing Mesh Renderers from enemy \"{__instance.name}\"");
                        break;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BlobAI), nameof(BlobAI.OnCollideWithPlayer))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> BlobAITransOnCollideWithPlayer(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 2; i < codes.Count; i++)
            {
                // fix erroneous < 0 check with <= 0
                if (codes[i].opcode == OpCodes.Bge_Un && codes[i - 2].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 2].operand == typeof(BlobAI).GetField("angeredTimer", BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    codes[i].opcode = OpCodes.Bgt_Un;
                    Plugin.Logger.LogDebug("Transpiler: Blob taming now possible without angering");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Hygrodere anger transpiler failed");
            return codes;
        }

        [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.Update))]
        [HarmonyPostfix]
        static void NutcrackerEnemyAIPostUpdate(NutcrackerEnemyAI __instance, bool ___isLeaderScript)
        {
            // if the leader is dead, manually update the clock
            if (___isLeaderScript && __instance.isEnemyDead)
                PrivateMembers.GLOBAL_NUTCRACKER_CLOCK.Invoke(__instance, null);
        }

        [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.Start))]
        [HarmonyPostfix]
        [HarmonyAfter("Dev1A3.LethalFixes")]
        static void NutcrackerEnemyAIPostStart(NutcrackerEnemyAI __instance, ref bool ___isLeaderScript, ref int ___previousPlayerSeenWhenAiming)
        {
            // 1 in vanilla, but LethalFixes makes it 0.5
            if (__instance.updatePositionThreshold < GlobalReferences.nutcrackerSyncDistance)
                GlobalReferences.nutcrackerSyncDistance = __instance.updatePositionThreshold;

            // fixes nutcracker tiptoe being early when against the host
            ___previousPlayerSeenWhenAiming = -1;

            // if numbersSpawned > 1, a leader might not have been assigned yet (if the first nutcracker spawned with another already queued in a vent)
            if (__instance.IsServer && !___isLeaderScript && __instance.enemyType.numberSpawned > 1)
            {
                NutcrackerEnemyAI[] nutcrackers = Object.FindObjectsOfType<NutcrackerEnemyAI>();
                foreach (NutcrackerEnemyAI nutcracker in nutcrackers)
                {
                    if (nutcracker != __instance && (bool)PrivateMembers.IS_LEADER_SCRIPT.GetValue(nutcracker))
                    {
                        Plugin.Logger.LogDebug($"NUTCRACKER CLOCK: Nutcracker #{__instance.GetInstanceID()} spawned, #{nutcracker.GetInstanceID()} is already leader");
                        return;
                    }
                }
                ___isLeaderScript = true;
                Plugin.Logger.LogInfo($"NUTCRACKER CLOCK: \"Leader\" is still unassigned, promoting #{__instance.GetInstanceID()}");
            }
        }

        // DEBUGGING: Verbose Nutcracker global clock logs
        /*
        static float prevInspection = -1f;

        [HarmonyPatch(typeof(NutcrackerEnemyAI), "GlobalNutcrackerClock")]
        [HarmonyPrefix]
        static void PreGlobalNutcrackerClock(bool ___isLeaderScript)
        {
            if (___isLeaderScript && Time.realtimeSinceStartup - NutcrackerEnemyAI.timeAtNextInspection > 2f)
                prevInspection = Time.realtimeSinceStartup;
        }

        [HarmonyPatch(typeof(NutcrackerEnemyAI), "GlobalNutcrackerClock")]
        [HarmonyPostfix]
        static void PostGlobalNutcrackerClock(NutcrackerEnemyAI __instance)
        {
            if (prevInspection >= 0f)
            {
                string status = __instance.isEnemyDead ? "dead" : "alive";
                Plugin.Logger.LogDebug($"NUTCRACKER CLOCK: Leader #{__instance.GetInstanceID()} is {status}, ticked at {prevInspection}, next tick at {NutcrackerEnemyAI.timeAtNextInspection + 2f}");
                prevInspection = -1f;
            }
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.SwitchToBehaviourStateOnLocalClient))]
        [HarmonyPrefix]
        static void PreSwitchToBehaviourStateOnLocalClient(EnemyAI __instance, int stateIndex)
        {
            if (__instance is NutcrackerEnemyAI && stateIndex == 1 && __instance.currentBehaviourStateIndex != 1)
                Plugin.Logger.LogDebug($"NUTCRACKER CLOCK: Nutcracker #{__instance.GetInstanceID()} began inspection at {Time.realtimeSinceStartup}, global inspection time is {NutcrackerEnemyAI.timeAtNextInspection}");
        }
        */

        [HarmonyPatch(typeof(EnemyAI), "SubtractFromPowerLevel")]
        [HarmonyPrefix]
        static void PreSubtractFromPowerLevel(EnemyAI __instance, ref bool ___removedPowerLevel)
        {
            if (__instance is MaskedPlayerEnemy)
            {
                // should always work correctly for the host?
                // I've only had mimickingPlayer desync on client
                if (!___removedPowerLevel && (__instance as MaskedPlayerEnemy).mimickingPlayer != null)
                {
                    Plugin.Logger.LogInfo("\"Masked\" was mimicking a player; will not subtract from power level");
                    ___removedPowerLevel = true;
                }
            }
            else if (__instance is ButlerEnemyAI)
            {
                if (Plugin.configMaskHornetsPower.Value)
                {
                    Plugin.Logger.LogInfo("Butler died, but mask hornets don't decrease power level");
                    ___removedPowerLevel = true;
                }
            }
        }

        [HarmonyPatch(typeof(FlowerSnakeEnemy), "SetFlappingLocalClient")]
        [HarmonyPostfix]
        public static void PostSetFlappingLocalClient(FlowerSnakeEnemy __instance, bool isMainSnake/*, bool setFlapping*/)
        {
            // if the current snake is dropping a player
            if (!isMainSnake /*|| setFlapping*/ || __instance.clingingToPlayer != GameNetworkManager.Instance.localPlayerController || !__instance.clingingToPlayer.disablingJetpackControls)
                return;

            for (int i = 0; i < __instance.clingingToPlayer.ItemSlots.Length; i++)
            {
                // if the item is equipped
                if (__instance.clingingToPlayer.ItemSlots[i] == null || __instance.clingingToPlayer.ItemSlots[i].isPocketed)
                    continue;

                if (__instance.clingingToPlayer.ItemSlots[i] is JetpackItem)
                {
                    // and is a jetpack that's activated
                    JetpackItem heldJetpack = __instance.clingingToPlayer.ItemSlots[i] as JetpackItem;
                    if ((bool)PrivateMembers.JETPACK_ACTIVATED.GetValue(heldJetpack))
                    {
                        __instance.clingingToPlayer.disablingJetpackControls = false;
                        __instance.clingingToPlayer.maxJetpackAngle = -1f;
                        __instance.clingingToPlayer.jetpackRandomIntensity = 0f;
                        Plugin.Logger.LogInfo("Player still using jetpack when tulip snake dropped; re-enable flight controls");
                        return;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.ReloadGunClientRpc))]
        [HarmonyPostfix]
        static void PostReloadGunClientRpc(NutcrackerEnemyAI __instance)
        {
            if (__instance.gun.shotgunShellLeft.enabled)
            {
                __instance.gun.shotgunShellLeft.enabled = false;
                __instance.gun.shotgunShellRight.enabled = false;
                __instance.gun.StartCoroutine(NonPatchFunctions.ShellsAppearAfterDelay(__instance.gun));
                Plugin.Logger.LogInfo("Shotgun was reloaded by nutcracker; animating shells");
            }
        }

        // prevents nullref if bunker spider is shot by nutcracker. bug originally described (and fixed) in NutcrackerFixes
        [HarmonyPatch(typeof(SandSpiderAI), nameof(SandSpiderAI.TriggerChaseWithPlayer))]
        [HarmonyPrefix]
        static bool SandSpiderAIPreTriggerChaseWithPlayer(PlayerControllerB playerScript)
        {
            return playerScript != null;
        }

        [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.Update))]
        [HarmonyPostfix]
        static void MaskedPlayerEnemyPostUpdate(MaskedPlayerEnemy __instance)
        {
            if (__instance.maskFloodParticle.isEmitting && __instance.inSpecialAnimationWithPlayer != null)
            {
                // bonus effect: cover the player's face with blood
                __instance.inSpecialAnimationWithPlayer.bodyBloodDecals[3].SetActive(true);
                // enables the blood spillage effect that Zeekerss removed in v49
                if (__instance.inSpecialAnimationWithPlayer == GameNetworkManager.Instance.localPlayerController && !HUDManager.Instance.HUDAnimator.GetBool("biohazardDamage"))
                {
                    HUDManager.Instance.HUDAnimator.SetBool("biohazardDamage", true);
                    Plugin.Logger.LogInfo("Enable screen blood for mask vomit animation");
                }
            }
        }

        [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.FinishKillAnimation))]
        [HarmonyPrefix]
        static void MaskedPlayerEnemyPreFinishKillAnimation(MaskedPlayerEnemy __instance)
        {
            // this should properly prevent the blood effect from persisting after you are rescued from a mask
            // reasons this didn't work in v49 (and presumably why it got removed):
            // - inSpecialAnimationWithPlayer was set to null before checking if it matched the local player
            // - just disabling biohazardDamage wasn't enough to transition back to a normal HUD animator state (it needs a trigger set as well)
            if (__instance.inSpecialAnimationWithPlayer == GameNetworkManager.Instance.localPlayerController && HUDManager.Instance.HUDAnimator.GetBool("biohazardDamage"))
            {
                // cancel the particle effect early, just in case (to prevent it from retriggering and becoming stuck)
                if (__instance.maskFloodParticle.isEmitting)
                    __instance.maskFloodParticle.Stop();
                HUDManager.Instance.HUDAnimator.SetBool("biohazardDamage", false);
                HUDManager.Instance.HUDAnimator.SetTrigger("HealFromCritical");
                Plugin.Logger.LogInfo("Vomit animation was interrupted while blood was on screen");
            }
        }

        [HarmonyPatch(typeof(RadMechAI), "Stomp")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> RadMechAITransStomp(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldloc_1 && codes[i + 1].opcode == OpCodes.Ldarg_S && codes[i + 2].opcode == OpCodes.Bge_Un)
                {
                    Label label = generator.DefineLabel();
                    for (int j = i + 3; j < codes.Count; j++)
                    {
                        if (codes[j].opcode == OpCodes.Dup)
                        {
                            codes[j - 1].labels.Add(label);
                            codes.InsertRange(i + 3, new CodeInstruction[]
                            {
                                new CodeInstruction(OpCodes.Ldloc_0),
                                new CodeInstruction(OpCodes.Ldfld, typeof(PlayerControllerB).GetField(nameof(PlayerControllerB.isInHangarShipRoom), BindingFlags.Instance | BindingFlags.Public)),
                                new CodeInstruction(OpCodes.Brtrue, label)
                            });
                            Plugin.Logger.LogDebug("Transpiler: Old Bird stomps don't damage players in ship");
                            return codes;
                        }
                    }
                }
            }

            Plugin.Logger.LogError("Old Bird stomp transpiler failed");
            return codes;
        }

        [HarmonyPatch(typeof(FlowermanAI), nameof(FlowermanAI.HitEnemy))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> FlowermanAITransHitEnemy(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 2; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Stfld && (FieldInfo)codes[i].operand == typeof(FlowermanAI).GetField(nameof(FlowermanAI.angerMeter), BindingFlags.Instance | BindingFlags.Public))
                {
                    for (int j = i - 2; j < codes.Count; j++)
                    {
                        if (codes[j].opcode == OpCodes.Ret)
                        {
                            Plugin.Logger.LogDebug("Transpiler: Remove bracken aggro on hit (replace with postfix)");
                            return codes;
                        }
                        codes[j].opcode = OpCodes.Nop;
                    }
                }
            }

            Plugin.Logger.LogError("Bracken damage transpiler failed");
            return codes;
        }

        [HarmonyPatch(typeof(FlowermanAI), nameof(FlowermanAI.HitEnemy))]
        [HarmonyPostfix]
        static void FlowermanAIPostHitEnemy(FlowermanAI __instance, PlayerControllerB playerWhoHit)
        {
            if (playerWhoHit != null)
            {
                __instance.angerMeter = 11f;
                __instance.angerCheckInterval = 1f;
            }
            else
                Plugin.Logger.LogInfo("Bracken was damaged by an enemy; don't max aggro");
            __instance.AddToAngerMeter(0.1f);
        }

        [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.HitEnemy))]
        [HarmonyPostfix]
        static void NutcrackerEnemyAIPostHitEnemy(NutcrackerEnemyAI __instance, PlayerControllerB playerWhoHit, bool ___aimingGun, bool ___reloadingGun, float ___timeSinceSeeingTarget)
        {
            if (playerWhoHit != null)
            {
                int id = (int)playerWhoHit.playerClientId;
                // "sense" the player hitting it, this allows turning while frozen in place
                if (__instance.IsOwner && !__instance.isEnemyDead && __instance.currentBehaviourStateIndex == 2 && !___aimingGun && !___reloadingGun && (id == __instance.lastPlayerSeenMoving || ___timeSinceSeeingTarget > 0.5f))
                    __instance.SwitchTargetServerRpc(id);
            }
        }

        [HarmonyPatch(typeof(NutcrackerEnemyAI), "AimGun", MethodType.Enumerator)]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> NutcrackerEnemyAITransAimGun(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 1; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_0 && codes[i + 1].opcode == OpCodes.Stfld && (FieldInfo)codes[i + 1].operand == typeof(NutcrackerEnemyAI).GetField("timesSeeingSamePlayer", BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    codes[i].opcode = OpCodes.Ldc_I4_1;
                    Plugin.Logger.LogDebug("Transpiler: Reset times nutcracker saw same player to 1, not 0");
                }
                else if (codes[i].opcode == OpCodes.Stfld && (FieldInfo)codes[i].operand == typeof(EnemyAI).GetField(nameof(EnemyAI.inSpecialAnimation), BindingFlags.Instance | BindingFlags.Public) && codes[i - 2].opcode == OpCodes.Ldloc_1)
                {
                    // instead of setting inSpecialAnimation to true
                    if (codes[i - 1].opcode == OpCodes.Ldc_I4_1)
                    {
                        // change updatePositionThreshold to a smaller value
                        codes[i - 1].opcode = OpCodes.Ldc_R4;
                        codes[i - 1].operand = 0.3f;
                        Plugin.Logger.LogDebug("Transpiler: Nutcracker will sync position while tiptoeing");
                    }
                    // instead of setting inSpecialAnimation to false
                    else
                    {
                        // change updatePositionThreshold to the original value
                        codes[i - 1].opcode = OpCodes.Ldsfld;
                        codes[i - 1].operand = typeof(GlobalReferences).GetField(nameof(GlobalReferences.nutcrackerSyncDistance), BindingFlags.Static | BindingFlags.NonPublic);
                        Plugin.Logger.LogDebug("Transpiler: Dynamic update threshold for nutcracker");
                    }
                    codes[i].operand = typeof(EnemyAI).GetField(nameof(EnemyAI.updatePositionThreshold), BindingFlags.Instance | BindingFlags.Public);
                }
            }

            return codes;
        }

        [HarmonyPatch(typeof(MouthDogAI), nameof(MouthDogAI.OnCollideWithPlayer))]
        [HarmonyPrefix]
        static bool MouthDogAIPreOnCollideWithPlayer(MouthDogAI __instance, Collider other)
        {
            if (__instance.isEnemyDead || (other.TryGetComponent(out PlayerControllerB player) && player.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed && !StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(__instance.transform.position)))
                return false;

            return true;
        }

        [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.KillEnemy))]
        [HarmonyPostfix]
        static void MaskedPlayerEnemyPostKillEnemy(MaskedPlayerEnemy __instance)
        {
            Animator mapDot = __instance.transform.Find("Misc/MapDot")?.GetComponent<Animator>();
            if (mapDot)
            {
                mapDot.enabled = false;
                Plugin.Logger.LogInfo("Stop animating masked radar dot");
            }
        }

        [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.SetSuit))]
        [HarmonyPostfix]
        static void PostSetSuit(MaskedPlayerEnemy __instance, int suitId)
        {
            Transform spine = __instance.animationContainer.Find("metarig/spine");
            if (spine == null)
                return;

            // on second thought, not sure parenting prefabs to NetworkObjects is stable or even supported
            try
            {
                if (suitId < StartOfRound.Instance.unlockablesList.unlockables.Count)
                {
                    UnlockableItem suit = StartOfRound.Instance.unlockablesList.unlockables[suitId];
                    if (suit.headCostumeObject != null)
                    {
                        Transform spine004 = spine.Find("spine.001/spine.002/spine.003/spine.004");
                        if (spine004 != null && spine004.Find(suit.headCostumeObject.name + "(Clone)") == null)
                        {
                            Object.Instantiate(suit.headCostumeObject, spine004.position, spine004.rotation, spine004);
                            Plugin.Logger.LogInfo($"Mimic #{__instance.GetInstanceID()} equipped {suit.unlockableName} head");
                        }
                    }
                    if (suit.lowerTorsoCostumeObject != null && spine.Find(suit.lowerTorsoCostumeObject.name + "(Clone)") == null)
                    {
                        Object.Instantiate(suit.lowerTorsoCostumeObject, spine.position, spine.rotation, spine);
                        Plugin.Logger.LogInfo($"Mimic #{__instance.GetInstanceID()} equipped {suit.unlockableName} torso");
                    }
                }
            }
            catch (System.Exception e)
            {
                Plugin.Logger.LogError("Encountered a non-fatal error while attaching costume pieces to mimic");
                Plugin.Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.SetEnemyOutside))]
        [HarmonyPostfix]
        static void MaskedPlayerEnemyPostSetEnemyOutside(MaskedPlayerEnemy __instance)
        {
            if (__instance.timeSinceSpawn > 40f)
                return;

            Transform spine003 = __instance.maskTypes[0].transform.parent.parent;

            Renderer betaBadgeMesh = spine003.Find("BetaBadge")?.GetComponent<Renderer>();
            if (betaBadgeMesh != null)
            {
                betaBadgeMesh.enabled = __instance.mimickingPlayer.playerBetaBadgeMesh.enabled;
                Plugin.Logger.LogInfo($"Mimic #{__instance.GetInstanceID()} VIP: {betaBadgeMesh.enabled}");
            }
            MeshFilter badgeMesh = spine003.Find("LevelSticker")?.GetComponent<MeshFilter>();
            if (badgeMesh != null)
            {
                badgeMesh.mesh = __instance.mimickingPlayer.playerBadgeMesh.mesh;
                Plugin.Logger.LogInfo($"Mimic #{__instance.GetInstanceID()} updated level sticker");
            }

            // toggling GameObjects under a NetworkObject maybe also a bad idea?
            try
            {
                foreach (DecalProjector bloodDecal in __instance.transform.GetComponentsInChildren<DecalProjector>())
                {
                    foreach (GameObject bodyBloodDecal in __instance.mimickingPlayer.bodyBloodDecals)
                    {
                        if (bloodDecal.name == bodyBloodDecal.name)
                        {
                            bloodDecal.gameObject.SetActive(bodyBloodDecal.activeSelf);
                            Plugin.Logger.LogInfo($"Mimic #{__instance.GetInstanceID()} blood: \"{bloodDecal.name}\"");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Plugin.Logger.LogError("Encountered a non-fatal error while enabling mimic blood");
                Plugin.Logger.LogError(e);
            }
        }

        [HarmonyPatch(typeof(DoorLock), "OnTriggerStay")]
        [HarmonyPrefix]
        public static bool DoorLockPreOnTriggerStay(Collider other)
        {
            // snare fleas and tulip snakes don't open door when latching to player
            return !(other.CompareTag("Enemy") && other.TryGetComponent(out EnemyAICollisionDetect enemyAICollisionDetect) && ((enemyAICollisionDetect.mainScript as CentipedeAI)?.clingingToPlayer != null || (enemyAICollisionDetect.mainScript as FlowerSnakeEnemy)?.clingingToPlayer != null));
        }

        [HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.SetMaskType))]
        [HarmonyPrefix]
        static bool PostSetMaskType(MaskedPlayerEnemy __instance, int maskType)
        {
            if (maskType == 5 && __instance.maskTypeIndex != 1)
            {
                __instance.maskTypeIndex = 1;
                Plugin.Logger.LogInfo("Mimic spawned that should be Tragedy");

                // replace the comedy mask's models with the tragedy models
                NonPatchFunctions.ConvertMaskToTragedy(__instance.maskTypes[0].transform);

                // and swap the sound files (these wouldn't work if the tragedy's GameObject was just toggled on)
                RandomPeriodicAudioPlayer randomPeriodicAudioPlayer = __instance.maskTypes[0].GetComponent<RandomPeriodicAudioPlayer>();
                if (randomPeriodicAudioPlayer != null)
                {
                    randomPeriodicAudioPlayer.randomClips = GlobalReferences.tragedyMaskRandomClips;
                    Plugin.Logger.LogInfo("Tragedy mimic cries");
                }
            }

            // need to replace the vanilla behavior entirely because it's just too buggy
            return false;
        }
    }
}
