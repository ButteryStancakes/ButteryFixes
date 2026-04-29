using ButteryFixes.Utility;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    static class CadaverPatches
    {
        static CadaverGrowthAI cadaverGrowthAI;

        [HarmonyPatch(typeof(CadaverBloomAI), nameof(CadaverBloomAI.KillEnemy))]
        [HarmonyPostfix]
        static void CadaverBloomAI_Post_KillEnemy(CadaverBloomAI __instance)
        {
            // fix leftover collision on scan nodes
            foreach (ScanNodeProperties scanNode in __instance.GetComponentsInChildren<ScanNodeProperties>())
            {
                if (scanNode.TryGetComponent(out Collider collider) && collider.enabled)
                {
                    collider.enabled = false;
                    Plugin.Logger.LogDebug($"Cadaver #{__instance.GetInstanceID()}: Fixed erroneous collision on \"{collider.name}\"");
                }
            }

            SkinnedMeshRenderer body = __instance.skinnedMeshRenderers.FirstOrDefault(skinnedMeshRenderer => skinnedMeshRenderer != null && skinnedMeshRenderer.name == "MeshLOD0");
            if (body != null && body.enabled)
            {
                body.enabled = false;
                body.forceRenderingOff = true;
                body.gameObject.SetActive(false);
                Plugin.Logger.LogDebug($"Cadaver #{__instance.GetInstanceID()}: Permanently disable main renderer");
            }
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.EnableEnemyMesh))]
        [HarmonyPrefix]
        static void CadaverBloomAI_Pre_EnableEnemyMesh(EnemyAI __instance, ref bool enable, bool overrideDoNotSet)
        {
            if (__instance is CadaverBloomAI)
            {
                // fix cadavers becoming visible again after death, especially near the ship
                if (__instance.isEnemyDead)
                    enable = false;

                if (overrideDoNotSet)
                {
                    // fix MapRadar renderers showing up on the bodycams
                    foreach (Renderer rend in __instance.meshRenderers)
                    {
                        if (rend == null)
                            continue;

                        if (rend.gameObject.layer == 14)
                        {
                            rend.enabled = false;
                            rend.forceRenderingOff = true;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.IncreaseBackFlowers))]
        [HarmonyPostfix]
        static void CadaverGrowthAI_Post_IncreaseBackFlowers(CadaverGrowthAI __instance, int playerId)
        {
            if (StartOfRound.Instance.allPlayerScripts[playerId] == GameNetworkManager.Instance.localPlayerController)
            {
                GlobalReferences.localPlayerHasBackFlowers = true;

                if (Configuration.scanImprovements.Value)
                    EnemyRadar.InfectLocalPlayer(__instance);
            }
        }

        [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.CurePlayer))]
        [HarmonyPostfix]
        static void CadaverGrowthAI_Post_CurePlayer(CadaverGrowthAI __instance, int playerId)
        {
            if (StartOfRound.Instance.allPlayerScripts[playerId] == GameNetworkManager.Instance.localPlayerController)
            {
                GlobalReferences.localPlayerHasBackFlowers = false;
                EnemyRadar.CureLocalPlayer();
            }
        }

        [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.InfectPlayer))]
        [HarmonyPostfix]
        static void CadaverGrowthAI_Post_InfectPlayer(CadaverGrowthAI __instance, PlayerControllerB playerScript)
        {
            if (StartOfRound.Instance.connectedPlayersAmount < 1 && playerScript == GameNetworkManager.Instance.localPlayerController && Configuration.scanImprovements.Value)
                EnemyRadar.InfectLocalPlayer(__instance);
        }

        [HarmonyPatch(typeof(CadaverBloomAI), nameof(CadaverBloomAI.Start))]
        [HarmonyPostfix]
        static void CadaverBloomAI_Post_Start(CadaverBloomAI __instance)
        {
            if (!Configuration.scanImprovements.Value)
                return;

            Transform mapDot = __instance.transform.Find("MapDot");

            if (mapDot == null)
                return;

            if (cadaverGrowthAI == null)
                cadaverGrowthAI = Object.FindAnyObjectByType<CadaverGrowthAI>();

            if (cadaverGrowthAI?.scanNodePrefab != null)
            {
                Transform mapDot2 = Object.Instantiate(cadaverGrowthAI.scanNodePrefab.transform.Find("MapDot (2)"), mapDot);
                mapDot2.SetLocalPositionAndRotation(new(-0.00283510843f, 1.10208738f, 0.00115406874f), Quaternion.Euler(-0.843f, -161.075f, -0.289f));
                mapDot2.localScale = new(0.0791011676f, 0.260565877f, 0.0807817727f);

                // don't add to meshRenderers? (so it leaves radar residue like other enemies)

                Plugin.Logger.LogDebug($"Cadaver #{__instance.GetInstanceID()}: Add red dot");
            }
        }

        [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.OnEnable))]
        [HarmonyPostfix]
        static void CadaverGrowthAI_Post_OnEnable(CadaverGrowthAI __instance)
        {
            if (cadaverGrowthAI == null)
                cadaverGrowthAI = __instance;
        }

        [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.OnDisable))]
        [HarmonyPostfix]
        static void CadaverGrowthAI_Post_OnDisable(CadaverGrowthAI __instance)
        {
            if (cadaverGrowthAI == __instance)
                cadaverGrowthAI = null;

            GlobalReferences.localPlayerHasBackFlowers = false;
        }

        [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.OnLocalPlayerTalk))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> CadaverGrowthAI_Trans_OnLocalPlayerTalk(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo backFlowers = AccessTools.Field(typeof(PlayerInfection), nameof(PlayerInfection.backFlowers));
            for (int i = 1; i < codes.Count - 3; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld && (FieldInfo)codes[i].operand == backFlowers && codes[i + 1].opcode == OpCodes.Ldnull && codes[i + 3].opcode == OpCodes.Brfalse)
                {
                    for (int j = i - 1; j <= i + 2; j++)
                    {
                        if (j != i)
                            codes[j].opcode = OpCodes.Nop;
                    }
                    codes[i].opcode = OpCodes.Ldsfld;
                    codes[i].operand = AccessTools.Field(typeof(GlobalReferences), nameof(GlobalReferences.localPlayerHasBackFlowers));
                    codes[i + 3].opcode = OpCodes.Brtrue;
                    Plugin.Logger.LogDebug("Transpiler (Cadaver voice): Restore spore cough");
                    return codes;
                }
            }

            Plugin.Logger.LogError("Cadaver voice transpiler failed");
            return instructions;
        }
    }
}
