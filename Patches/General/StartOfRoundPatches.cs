using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch]
    internal class StartOfRoundPatches
    {
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        static void StartOfRoundPostAwake(StartOfRound __instance)
        {
            ScriptableObjectOverrides.OverrideSelectableLevels();

            GlobalReferences.dopplerLevelMult = Plugin.configMusicDopplerLevel.Value switch
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
                if (terminal != null && terminal.orderedItemsFromTerminal.Count > 0)
                {
                    terminal.orderedItemsFromTerminal.Clear();
                    terminal.SyncGroupCreditsServerRpc(terminal.groupCredits, 0);
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
                    terminal.orderedItemsFromTerminal = ES3.Load("ButteryFixes_DeliveryItems", GameNetworkManager.Instance.currentSaveFileName, new List<int>());
                    terminal.numberOfItemsInDropship = terminal.orderedItemsFromTerminal.Count;
                    if (terminal.numberOfItemsInDropship > 0)
                    {
                        Plugin.Logger.LogInfo($"Dropship inventory was restocked from save file ({terminal.numberOfItemsInDropship} items):");
                        for (int i = 0; i < terminal.numberOfItemsInDropship; i++)
                            Plugin.Logger.LogInfo($"#{i + 1} - {terminal.buyableItemsList[terminal.orderedItemsFromTerminal[i]].itemName}");
                    }
                }
                catch (System.Exception e)
                {
                    Plugin.Logger.LogError($"An error occurred while fetching dropship inventory from save file \"{GameNetworkManager.Instance.currentSaveFileName}\"");
                    Plugin.Logger.LogError(e);
                }
            }
        }
    }
}
