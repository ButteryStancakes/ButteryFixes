﻿using ButteryFixes.Utility;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(JetpackItem))]
    internal class JetpackPatches
    {
        [HarmonyPatch(nameof(JetpackItem.ExplodeJetpackClientRpc))]
        [HarmonyPostfix]
        public static void JetpackItem_Post_ExplodeJetpackClientRpc(JetpackItem __instance)
        {
            if (!Configuration.playermodelPatches.Value)
                return;

            DeadBodyInfo playerBody = __instance.previousPlayerHeldBy.deadBody;

            if (playerBody == null)
            {
                foreach (DeadBodyInfo deadBodyInfo in Object.FindObjectsByType<DeadBodyInfo>(FindObjectsSortMode.None))
                {
                    if (deadBodyInfo.playerScript == __instance.previousPlayerHeldBy)
                        playerBody = deadBodyInfo;
                }
            }

            if (playerBody != null)
            {
                if (GlobalReferences.scavengerSuitBurnt != null)
                {
                    playerBody.setMaterialToPlayerSuit = false;
                    foreach (Renderer rend in playerBody.GetComponentsInChildren<Renderer>())
                    {
                        if (rend.gameObject.layer == 0 && (rend.name.StartsWith("BetaBadge") || rend.name.StartsWith("LevelSticker") || rend.name.StartsWith("BirthdayHat")))
                            rend.forceRenderingOff = true;
                        // don't change ParticleSystem renderers
                        else if (rend.gameObject.layer == 20 && (rend is SkinnedMeshRenderer || rend is MeshRenderer))
                            rend.sharedMaterial = GlobalReferences.scavengerSuitBurnt;
                    }
                    NonPatchFunctions.SmokingHotCorpse(playerBody.transform);
                    Plugin.Logger.LogDebug("Jetpack exploded and burned player corpse");
                }
            }
            else
            {
                Plugin.Logger.LogWarning("Jetpack exploded but the player that crashed it didn't spawn a body");
                if (__instance.previousPlayerHeldBy == GameNetworkManager.Instance.localPlayerController)
                {
                    GlobalReferences.crashedJetpackAsLocalPlayer = true;
                    Plugin.Logger.LogInfo("Local player crashed, try to run other patch when corpse is spawned");
                }
            }
        }
    }
}
