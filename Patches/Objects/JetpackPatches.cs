using ButteryFixes.Utility;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class JetpackPatches
    {
        [HarmonyPatch(typeof(JetpackItem), nameof(JetpackItem.ExplodeJetpackClientRpc))]
        [HarmonyPostfix]
        public static void PostExplodeJetpackClientRpc(JetpackItem __instance, PlayerControllerB ___previousPlayerHeldBy)
        {
            if (Compatibility.DISABLE_PLAYERMODEL_PATCHES)
                return;

            DeadBodyInfo playerBody = ___previousPlayerHeldBy.deadBody;

            if (playerBody == null)
            {
                foreach (DeadBodyInfo deadBodyInfo in Object.FindObjectsOfType<DeadBodyInfo>())
                {
                    if (deadBodyInfo.playerScript == ___previousPlayerHeldBy)
                        playerBody = deadBodyInfo;
                }
            }

            if (playerBody != null)
            {
                playerBody.setMaterialToPlayerSuit = false;
                foreach (Renderer rend in playerBody.GetComponentsInChildren<Renderer>())
                {
                    if (rend.gameObject.layer == 0 && (rend.name.StartsWith("BetaBadge") || rend.name.StartsWith("LevelSticker")))
                        rend.forceRenderingOff = true;
                    // don't change ParticleSystem renderers
                    else if (rend.gameObject.layer == 20 && (rend is SkinnedMeshRenderer || rend is MeshRenderer))
                        rend.sharedMaterial = GlobalReferences.scavengerSuitBurnt;
                }
                NonPatchFunctions.SmokingHotCorpse(playerBody.transform);
                Plugin.Logger.LogDebug("Jetpack exploded and burned player corpse");
            }
            else
            {
                Plugin.Logger.LogWarning("Jetpack exploded but the player that crashed it didn't spawn a body");
                if (___previousPlayerHeldBy == GameNetworkManager.Instance.localPlayerController)
                {
                    GlobalReferences.crashedJetpackAsLocalPlayer = true;
                    Plugin.Logger.LogInfo("Local player crashed, try to run other patch when corpse is spawned");
                }
            }
        }
    }
}
