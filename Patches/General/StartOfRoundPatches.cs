using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch]
    internal class StartOfRoundPatches
    {
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyBefore(Compatibility.GUID_GENERAL_IMPROVEMENTS)]
        [HarmonyPostfix]
        static void StartOfRoundPostAwake(StartOfRound __instance)
        {
            ScriptableObjectOverrides.OverrideSelectableLevels();

            GlobalReferences.dopplerLevelMult = Configuration.musicDopplerLevel.Value switch
            {
                MusicDopplerLevel.None => 0f,
                MusicDopplerLevel.Reduced => 0.333f,
                _ => 1f,
            };
            __instance.speakerAudioSource.dopplerLevel = GlobalReferences.dopplerLevelMult;
            Plugin.Logger.LogDebug("Doppler level: Ship speaker");

            GlobalReferences.playerBody = StartOfRound.Instance.playerRagdolls[0].GetComponent<SkinnedMeshRenderer>().sharedMesh;
            GlobalReferences.scavengerSuitBurnt = StartOfRound.Instance.playerRagdolls[6].GetComponent<SkinnedMeshRenderer>().sharedMaterial;
            GlobalReferences.smokeParticle = StartOfRound.Instance.playerRagdolls[6].transform.Find("SmokeParticle")?.gameObject;

            ScriptableObjectOverrides.OverrideItems();
            AudioSource stickyNote = __instance.elevatorTransform.Find("StickyNoteItem")?.GetComponent<AudioSource>();
            if (stickyNote != null)
            {
                stickyNote.rolloffMode = AudioRolloffMode.Linear;
                stickyNote.GetComponent<PhysicsProp>().scrapValue = 0;
                Plugin.Logger.LogDebug($"Audio rolloff: Sticky note");
            }
            AudioSource clipboard = __instance.elevatorTransform.Find("ClipboardManual")?.GetComponent<AudioSource>();
            if (clipboard != null)
            {
                clipboard.rolloffMode = AudioRolloffMode.Linear;
                clipboard.GetComponent<ClipboardItem>().scrapValue = 0;
                Plugin.Logger.LogDebug($"Audio rolloff: Clipboard");
            }

            ScriptableObjectOverrides.OverrideUnlockables();

            GlobalReferences.shipAnimator = __instance.shipAnimatorObject.GetComponent<Animator>();

            __instance.VehiclesList.FirstOrDefault(vehicle => vehicle.name == "CompanyCruiser").GetComponent<VehicleController>().radioAudio.dopplerLevel = Configuration.musicDopplerLevel.Value == MusicDopplerLevel.Reduced ? 0.37f : GlobalReferences.dopplerLevelMult;
            Plugin.Logger.LogDebug("Doppler level: Cruiser");

            ParticleSystem windParticle = __instance.elevatorTransform.GetComponent<PlayAudioAnimationEvent>()?.particle;
            if (windParticle)
            {
                ParticleSystem.MainModule main = windParticle.main;
                main.stopAction = ParticleSystemStopAction.None;
                Plugin.Logger.LogDebug("Orbit visuals: Particles");
            }

            BeltBagInventoryUI beltBagUI = Object.FindAnyObjectByType<BeltBagInventoryUI>(FindObjectsInactive.Include);
            if (beltBagUI != null)
            {
                Navigation nav = new()
                {
                    mode = Navigation.Mode.None
                };
                foreach (Button button in beltBagUI.GetComponentsInChildren<Button>())
                    button.navigation = nav;
            }

            __instance.mapScreen.mapCameraAnimator.transform.localPosition = new(0f, 0f, -0.95f);
            Plugin.Logger.LogDebug("Orbit visuals: Camera flash");

            Camera shipCam = __instance.elevatorTransform.Find("Cameras/ShipCamera")?.GetComponent<Camera>();
            if (shipCam != null)
            {
                shipCam.cullingMask |= (1 << LayerMask.NameToLayer("DecalStickableSurface"));
                Plugin.Logger.LogDebug("Orbit visuals: Terminal on CCTV");
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetStats))]
        [HarmonyPostfix]
        static void PostResetStats(StartOfRound __instance)
        {
            // stop tracking "most profitable" between days
            for (int i = 0; i < __instance.gameStats.allPlayerStats.Length; i++)
                __instance.gameStats.allPlayerStats[i].profitable = 0;
            Plugin.Logger.LogDebug("Cleared \"profitable\" stat for all employees");
        }

        [HarmonyPatch(typeof(StartOfRound), "ResetShipFurniture")]
        [HarmonyPostfix]
        static void PostResetShipFurniture(StartOfRound __instance)
        {
            Terminal terminal = GlobalReferences.Terminal;
            if (__instance.IsServer)
            {
                // empty the dropship on game over
                if (terminal != null)
                {
                    if (terminal.orderedItemsFromTerminal.Count > 0)
                    {
                        terminal.orderedItemsFromTerminal.Clear();
                        terminal.SyncGroupCreditsServerRpc(terminal.groupCredits, 0);
                    }
                    terminal.orderedVehicleFromTerminal = -1;
                    terminal.vehicleInDropship = false;
                    Plugin.Logger.LogInfo("Dropship inventory was emptied (game over)");
                }
                if (__instance.magnetOn)
                {
                    if (__instance.isObjectAttachedToMagnet && __instance.attachedVehicle != null && __instance.attachedVehicle.TryGetComponent(out NetworkObject netObj) && netObj.IsSpawned)
                    {
                        __instance.isObjectAttachedToMagnet = false;
                        foreach (GrabbableObject grabObj in Object.FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None))
                        {
                            if (grabObj.transform.parent == __instance.attachedVehicle.transform && grabObj.TryGetComponent(out NetworkObject netObj2) && netObj2.IsSpawned)
                            {
                                netObj2.Despawn();
                                Plugin.Logger.LogDebug($"Deleted \"{grabObj.itemProperties.itemName}\" #{grabObj.GetInstanceID()} (inside Cruiser marked for deletion)");
                            }
                        }
                        netObj.Despawn();
                        Plugin.Logger.LogInfo("Deleted Cruiser (game over)");
                    }
                    __instance.magnetLever.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
                }
            }
            if (terminal != null)
                terminal.hasWarrantyTicket = false;
            // reset TZP between challenge moon attempts
            if (__instance.isChallengeFile)
            {
                for (int i = 0; i < __instance.allPlayerScripts.Length; i++)
                {
                    __instance.allPlayerScripts[i].drunkness = 0f;
                    __instance.allPlayerScripts[i].drunknessInertia = 0f;
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), "LoadShipGrabbableItems")]
        [HarmonyPostfix]
        static void PostLoadShipGrabbableItems()
        {
            Terminal terminal = GlobalReferences.Terminal;
            // reload the dropship's contents from the save file, if any exist
            if (terminal != null)
            {
                try
                {
                    terminal.orderedVehicleFromTerminal = ES3.Load("ButteryFixes_DeliveryVehicle", GameNetworkManager.Instance.currentSaveFileName, -1);
                    if (terminal.orderedVehicleFromTerminal >= 0)
                    {
                        terminal.vehicleInDropship = true;
                        Plugin.Logger.LogInfo($"Dropship inventory was restocked from save file (Vehicle: {terminal.buyableVehicles[terminal.orderedVehicleFromTerminal].vehicleDisplayName})");
                    }
                    else
                    {
                        terminal.orderedItemsFromTerminal = ES3.Load("ButteryFixes_DeliveryItems", GameNetworkManager.Instance.currentSaveFileName, new List<int>());
                        terminal.numberOfItemsInDropship = terminal.orderedItemsFromTerminal.Count;
                        if (terminal.numberOfItemsInDropship > 0)
                        {
                            Plugin.Logger.LogInfo($"Dropship inventory was restocked from save file ({terminal.numberOfItemsInDropship} items):");
                            for (int i = 0; i < terminal.numberOfItemsInDropship; i++)
                                Plugin.Logger.LogInfo($"#{i + 1} - {terminal.buyableItemsList[terminal.orderedItemsFromTerminal[i]].itemName}");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Plugin.Logger.LogError($"An error occurred while fetching dropship inventory from save file \"{GameNetworkManager.Instance.currentSaveFileName}\"");
                    Plugin.Logger.LogError(e);
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), "SetTimeAndPlanetToSavedSettings")]
        [HarmonyPrefix]
        static void PreSetTimeAndPlanetToSavedSettings()
        {
            if (!Compatibility.INSTALLED_GENERAL_IMPROVEMENTS && Configuration.randomizeDefaultSeed.Value && GameNetworkManager.Instance.currentSaveFileName != "LCChallengeFile" && !ES3.KeyExists("RandomSeed", GameNetworkManager.Instance.currentSaveFileName))
            {
                ES3.Save("RandomSeed", Random.Range(1, 100000000), GameNetworkManager.Instance.currentSaveFileName);
                Plugin.Logger.LogInfo("Re-rolled starting seed");
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ReviveDeadPlayers))]
        [HarmonyPostfix]
        static void PostReviveDeadPlayers()
        {
            GlobalReferences.crashedJetpackAsLocalPlayer = false;
            SoundManager.Instance.SetEchoFilter(false);
        }

        [HarmonyPatch(typeof(StartOfRound), "Start")]
        [HarmonyPostfix]
        static void StartOfRoundPostStart(StartOfRound __instance)
        {
            if (!__instance.IsServer && __instance.inShipPhase && !GameNetworkManager.Instance.gameHasStarted)
            {
                foreach (GrabbableObject grabbableObject in Object.FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None))
                {
                    grabbableObject.scrapPersistedThroughRounds = true;
                    grabbableObject.isInElevator = true;
                    grabbableObject.isInShipRoom = true;
                    grabbableObject.hasBeenHeld = true;

                    LungProp lungProp = grabbableObject as LungProp;
                    if (lungProp != null && lungProp.isLungDocked)
                    {
                        Plugin.Logger.LogDebug("Player late-joined a lobby with a powered apparatus");
                        lungProp.isLungDocked = false;
                        lungProp.GetComponent<AudioSource>().Stop();
                    }
                }
                Plugin.Logger.LogDebug("Mark all scrap already in the ship as collected");
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ReviveDeadPlayers))]
        [HarmonyPrefix]
        static void PreReviveDeadPlayers(StartOfRound __instance)
        {
            if (Compatibility.INSTALLED_GENERAL_IMPROVEMENTS)
                return;

            for (int i = 0; i < __instance.allPlayerScripts.Length; i++)
            {
                if (__instance.allPlayerScripts[i].isPlayerDead)
                    NonPatchFunctions.ForceRefreshAllHelmetLights(__instance.allPlayerScripts[i], true);
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LateUpdate))]
        [HarmonyPostfix]
        static void StartOfRoundPostLateUpdate(StartOfRound __instance)
        {
            if (!Compatibility.INSTALLED_GENERAL_IMPROVEMENTS && GlobalReferences.shipNode != null)
                GlobalReferences.shipNode.position = StartOfRound.Instance.elevatorTransform.position + GlobalReferences.shipNodeOffset;

            if (SoundManager.Instance != null && SoundManager.Instance.echoEnabled && GameNetworkManager.Instance.localPlayerController != null && GameNetworkManager.Instance.localPlayerController.isPlayerDead && GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null && !GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript.isInsideFactory)
                SoundManager.Instance.SetEchoFilter(false);
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipHasLeft))]
        [HarmonyPostfix]
        static void PostShipHasLeft(StartOfRound __instance)
        {
            // this needs to run before the scene unloads or it will miss the apparatus
            GlobalReferences.scrapNotCollected = 0;
            foreach (GrabbableObject grabbableObject in Object.FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None))
            {
                NetworkObject networkObject = grabbableObject.GetComponent<NetworkObject>();
                if (networkObject == null || !networkObject.IsSpawned)
                    continue;

                if (grabbableObject.itemProperties.isScrap && grabbableObject.scrapValue > 0 && ((grabbableObject is not GiftBoxItem && grabbableObject.deactivated) || (!grabbableObject.isInShipRoom && !grabbableObject.isInElevator && !grabbableObject.isHeld) || grabbableObject.isInFactory) && !grabbableObject.scrapPersistedThroughRounds && grabbableObject is not RagdollGrabbableObject)
                {
                    GlobalReferences.scrapNotCollected += grabbableObject.scrapValue;
                    //Plugin.Logger.LogDebug($"Did not collect: {grabbableObject.itemProperties.itemName} (${grabbableObject.scrapValue})");
                }
            }
            // unkilled butlers are still worth the knife they didn't drop
            foreach (ButlerEnemyAI butlerEnemyAI in Object.FindObjectsByType<ButlerEnemyAI>(FindObjectsSortMode.None))
            {
                if (!butlerEnemyAI.isEnemyDead)
                {
                    KnifeItem knife = butlerEnemyAI.knifePrefab?.GetComponent<KnifeItem>();
                    if (knife != null)
                    {
                        GlobalReferences.scrapNotCollected += knife.scrapValue;
                        //Plugin.Logger.LogDebug($"Did not kill Butler (${knife.scrapValue})");
                    }
                }
            }

            if (__instance.currentLevel.name != "CompanyBuildingLevel")
                TimeOfDay.Instance.playDelayedMusicCoroutine = null;
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetShip))]
        [HarmonyPostfix]
        static void PostResetShip(StartOfRound __instance)
        {
            // fix Experimentation weather on screen after being fired
            if (!__instance.isChallengeFile)
                __instance.SetMapScreenInfoToCurrentLevel();
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ReviveDeadPlayers))]
        [HarmonyPostfix]
        static void PostReviveDeadPlayers(StartOfRound __instance)
        {
            // stop bleeding if you're healed at the end of the round
            for (int i = 0; i < __instance.allPlayerScripts.Length; i++)
            {
                if (__instance.allPlayerScripts[i].criticallyInjured || __instance.allPlayerScripts[i].bleedingHeavily)
                {
                    __instance.allPlayerScripts[i].criticallyInjured = false;
                    __instance.allPlayerScripts[i].bleedingHeavily = false;
                    Plugin.Logger.LogDebug($"Fixed player #{i} ({__instance.allPlayerScripts[i].playerUsername}) still bleeding after recovering full HP");
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), "PositionSuitsOnRack")]
        [HarmonyPostfix]
        static void PostPositionSuitsOnRack()
        {
            UnlockableSuit[] unlockableSuits = Object.FindObjectsByType<UnlockableSuit>(FindObjectsSortMode.None);
            /*if (unlockableSuits.Length > 1)
            {*/
                foreach (UnlockableSuit unlockableSuit in unlockableSuits)
                {
                    if (unlockableSuit.syncedSuitID.Value == 0)
                    {
                        unlockableSuit.gameObject.GetComponent<InteractTrigger>().hoverTip = "Change: " + StartOfRound.Instance.unlockablesList.unlockables[unlockableSuit.suitID].unlockableName;
                        return;
                    }
                }    
            //}
        }
    }
}
