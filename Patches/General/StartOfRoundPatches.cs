using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch]
    internal class StartOfRoundPatches
    {
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyBefore(Compatibility.GENERAL_IMPROVEMENTS_GUID)]
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
            Plugin.Logger.LogInfo("Doppler level: Ship speaker");

            GameObject tragedyRagdoll = StartOfRound.Instance.playerRagdolls.FirstOrDefault(playerRagdoll => playerRagdoll.name == "PlayerRagdollWithTragedyMask Variant");
            if (tragedyRagdoll != null)
            {
                // cache all of the visual references to the tragedy mask (the item and enemy prefabs are broken, only the ragdoll has all the correct assets)
                foreach (MeshFilter meshFilter in tragedyRagdoll.GetComponentsInChildren<MeshFilter>())
                {
                    switch (meshFilter.name)
                    {
                        case "Mesh":
                            GlobalReferences.tragedyMask = meshFilter.sharedMesh;
                            GlobalReferences.tragedyMaskMat = meshFilter.GetComponent<MeshRenderer>()?.sharedMaterial;
                            break;
                        case "ComedyMaskLOD1":
                            GlobalReferences.tragedyMaskLOD = meshFilter.sharedMesh;
                            break;
                        case "EyesFilled":
                            GlobalReferences.tragedyMaskEyesFilled = meshFilter.sharedMesh;
                            break;
                    }
                }
            }

            GlobalReferences.playerBody = StartOfRound.Instance.playerRagdolls[0].GetComponent<SkinnedMeshRenderer>().sharedMesh;
            GlobalReferences.scavengerSuitBurnt = StartOfRound.Instance.playerRagdolls[6].GetComponent<SkinnedMeshRenderer>().sharedMaterial;

            ScriptableObjectOverrides.OverrideItems();
            AudioSource stickyNote = __instance.elevatorTransform.Find("StickyNoteItem")?.GetComponent<AudioSource>();
            if (stickyNote != null)
            {
                stickyNote.rolloffMode = AudioRolloffMode.Linear;
                stickyNote.GetComponent<PhysicsProp>().scrapValue = 0;
                Plugin.Logger.LogInfo($"Audio rolloff: Sticky note");
            }
            AudioSource clipboard = __instance.elevatorTransform.Find("ClipboardManual")?.GetComponent<AudioSource>();
            if (clipboard != null)
            {
                clipboard.rolloffMode = AudioRolloffMode.Linear;
                clipboard.GetComponent<ClipboardItem>().scrapValue = 0;
                Plugin.Logger.LogInfo($"Audio rolloff: Clipboard");
            }

            ScriptableObjectOverrides.OverrideUnlockables();

            GlobalReferences.shipAnimator = __instance.shipAnimatorObject.GetComponent<Animator>();
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetStats))]
        [HarmonyPostfix]
        static void StartOfRoundPostResetStats(StartOfRound __instance)
        {
            // stop tracking "most profitable" between days
            for (int i = 0; i < __instance.gameStats.allPlayerStats.Length; i++)
                __instance.gameStats.allPlayerStats[i].profitable = 0;
            Plugin.Logger.LogInfo("Cleared \"profitable\" stat for all employees");
        }

        [HarmonyPatch(typeof(StartOfRound), "ResetShipFurniture")]
        [HarmonyPostfix]
        static void PostResetShipFurniture(StartOfRound __instance)
        {
            if (__instance.IsServer)
            {
                Terminal terminal = Object.FindObjectOfType<Terminal>();
                // empty the dropship on game over
                if (terminal != null)
                {
                    if (terminal.orderedItemsFromTerminal.Count > 0)
                    {
                        terminal.orderedItemsFromTerminal.Clear();
                        terminal.SyncGroupCreditsServerRpc(terminal.groupCredits, 0);
                    }
                    terminal.orderedVehicleFromTerminal = -1;
                    terminal.hasWarrantyTicket = false;
                    terminal.vehicleInDropship = false;
                    Plugin.Logger.LogInfo("Dropship inventory was emptied (game over)");
                }
            }
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
            Terminal terminal = Object.FindObjectOfType<Terminal>();
            // reload the dropship's contents from the save file, if any exist
            if (terminal != null)
            {
                try
                {
                    terminal.orderedVehicleFromTerminal = ES3.Load("ButteryFixes_DeliveryVehicle", GameNetworkManager.Instance.currentSaveFileName, -1);
                    if (terminal.orderedVehicleFromTerminal >= 0f)
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
        }

        [HarmonyPatch(typeof(StartOfRound), "Start")]
        [HarmonyPostfix]
        static void StartOfRoundPostStart(StartOfRound __instance)
        {
            if (!__instance.IsServer && __instance.inShipPhase && !GameNetworkManager.Instance.gameHasStarted)
            {
                foreach (GrabbableObject grabbableObject in Object.FindObjectsOfType<GrabbableObject>())
                {
                    grabbableObject.scrapPersistedThroughRounds = true;
                    grabbableObject.isInElevator = true;
                    grabbableObject.isInShipRoom = true;

                    LungProp lungProp = grabbableObject as LungProp;
                    if (lungProp != null && lungProp.isLungDocked)
                    {
                        Plugin.Logger.LogInfo("Player late-joined a lobby with a powered apparatus");
                        lungProp.isLungDocked = false;
                        lungProp.GetComponent<AudioSource>().Stop();
                    }
                }
                Plugin.Logger.LogInfo("Mark all scrap in the ship as collected (late join)");
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
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipHasLeft))]
        [HarmonyPostfix]
        static void PostShipHasLeft(StartOfRound __instance)
        {
            // this needs to run before the scene unloads or it will miss the apparatus
            GlobalReferences.scrapNotCollected = 0;
            foreach (GrabbableObject grabbableObject in Object.FindObjectsOfType<GrabbableObject>())
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
            foreach (ButlerEnemyAI butlerEnemyAI in Object.FindObjectsOfType<ButlerEnemyAI>())
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
        }
    }
}
