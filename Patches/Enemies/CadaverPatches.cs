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
        static void CadaverGrowthAI_Post_CurePlayer(int playerId)
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

            if (GlobalReferences.cadaverGrowthAI == null)
                GlobalReferences.cadaverGrowthAI = Object.FindAnyObjectByType<CadaverGrowthAI>();

            if (GlobalReferences.cadaverGrowthAI?.scanNodePrefab != null)
            {
                Transform mapDot2 = Object.Instantiate(GlobalReferences.cadaverGrowthAI.scanNodePrefab.transform.Find("MapDot (2)"), mapDot);
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
            if (GlobalReferences.cadaverGrowthAI == null)
                GlobalReferences.cadaverGrowthAI = __instance;
        }

        [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.OnDisable))]
        [HarmonyPostfix]
        static void CadaverGrowthAI_Post_OnDisable(CadaverGrowthAI __instance)
        {
            if (GlobalReferences.cadaverGrowthAI == __instance)
                GlobalReferences.cadaverGrowthAI = null;

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

        [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.ProgressPlayerInfections))]
        [HarmonyPostfix]
        static void CadaverGrowthAI_Post_ProgressPlayerInfections(CadaverGrowthAI __instance)
        {
            int playerId = (int)GameNetworkManager.Instance.localPlayerController.playerClientId;

            if (__instance.playerInfections.Length <= playerId)
                return;

            if (__instance.playerInfections[playerId].burstMeter >= 0.9f)
            {
                if ((StartOfRound.Instance.livingPlayers > 1 && Configuration.cadaverHUD.Value) || (StartOfRound.Instance.shipIsLeaving && StartOfRound.Instance.livingPlayers <= 1))
                    HUDManager.Instance.DisplayStatusEffect("HIGH FEVER DETECTED!!!\nFOREIGN BODIES DETECTED!!!\nIRREGULAR BRAINWAVE DETECTED!!!");

                if (StartOfRound.Instance.shipIsLeaving && TimeOfDay.Instance.currentDayTimeStarted)
                {
                    float burstMeter = Mathf.Clamp(__instance.playerInfections[playerId].burstMeter + (0.058f * Time.deltaTime), 0.9f, 0.996f);
                    if (burstMeter < 1f)
                        __instance.playerInfections[playerId].burstMeter = burstMeter;

                    HUDManager.Instance.cadaverFilter = Mathf.Lerp(0f, 1f, (burstMeter - 0.9f) / 0.1f);
                    SoundManager.Instance.alternateEarsRinging = true;
                    SoundManager.Instance.earsRingingTimer = 1f;
                }
            }
        }

        [HarmonyPatch(typeof(CadaverBloomAI), nameof(CadaverBloomAI.Start))]
        [HarmonyPatch(typeof(CadaverBloomAI), nameof(CadaverBloomAI.DoBurstAnimation), MethodType.Enumerator)]
        [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.Debug_SetWeedCount))]
        [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.Debug_CadaverBloomBurstPlayer))]
        [HarmonyPatch(typeof(ShowerTrigger), nameof(ShowerTrigger.AddPlayerToShower))]
        [HarmonyPatch(typeof(ShowerTrigger), nameof(ShowerTrigger.RemovePlayerFromShower))]
        [HarmonyPatch(typeof(ShowerTrigger), nameof(ShowerTrigger.Update))]
        //[HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.LateUpdate))]
        //[HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.TrySprayingWeedKillerOnLocalPlayer))]
        //[HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.CheckForCadaverPlantsInSprayPath))]
        //[HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.KillCadaverPlantRpc))]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.bloomPlayerOnDelay), MethodType.Enumerator)]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> CacheCadaverGrowthAI(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && codes[i].operand as MethodInfo == ReflectionCache.FIND_OBJECT_OF_TYPE_CADAVER_GROWTH_AI)
                {
                    codes[i].opcode = OpCodes.Ldsfld;
                    codes[i].operand = ReflectionCache.CADAVER_GROWTH_AI;
                    Plugin.Logger.LogDebug($"Transpiler ({__originalMethod.DeclaringType}.{__originalMethod.Name}): Cache Cadaver script");
                }
            }

            //Plugin.Logger.LogWarning($"{__originalMethod.Name} transpiler failed");
            return codes;
        }

        [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.CoughSporesRpc))]
        [HarmonyPrefix]
        static void CadaverGrowthAI_Pre_CoughSporesRpc()
        {
            GlobalReferences.playerJustCoughed = true;
        }

        [HarmonyPatch(typeof(CadaverGrowthAI), nameof(CadaverGrowthAI.CoughSporesRpc))]
        [HarmonyPostfix]
        static void CadaverGrowthAI_Post_CoughSporesRpc()
        {
            GlobalReferences.playerJustCoughed = false;
        }
    }
}
