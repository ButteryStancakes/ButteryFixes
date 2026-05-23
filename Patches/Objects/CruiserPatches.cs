using BepInEx.Bootstrap;
using ButteryFixes.Utility;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(VehicleController))]
    static class CruiserPatches
    {
        static float radioPingTimestamp;

        [HarmonyPatch(nameof(VehicleController.DestroyCar))]
        [HarmonyPostfix]
        static void VehicleController_Post_DestroyCar(VehicleController __instance)
        {
            __instance.hoodAudio.mute = true;
            __instance.healthMeter.GetComponentInChildren<Renderer>().forceRenderingOff = true;
            //__instance.turboMeter.GetComponentInChildren<Renderer>().forceRenderingOff = true;

            if (StartOfRound.Instance.attachedVehicle == __instance)
                StartOfRound.Instance.attachedVehicle = null;
        }

        [HarmonyPatch(nameof(VehicleController.SetCarEffects))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> VehicleController_Trans_SetCarEffects(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            MethodInfo playAudibleNoise = AccessTools.Method(typeof(RoundManager), nameof(RoundManager.PlayAudibleNoise));
            for (int i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand as MethodInfo == playAudibleNoise)
                {
                    for (int j = i + 1; j < codes.Count; j++)
                    {
                        if (codes[j].opcode == OpCodes.Br)
                        {
                            codes.Insert(i + 1, new(OpCodes.Br, codes[j].operand));
                            Plugin.Logger.LogDebug("Transpiler (Cruiser effects): Fix 2 audible noises at once");
                            return codes;
                        }
                    }
                }
            }

            Plugin.Logger.LogError("Cruiser effects transpiler failed");
            return instructions;
        }

        [HarmonyPatch(nameof(VehicleController.CollectItemsInTruck))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> VehicleController_Trans_CollectItemsInTruck(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            bool patchedTriggerInteraction = Chainloader.PluginInfos.ContainsKey(Compatibility.GUID_CRUISER_ADDITIONS), creditLastDriver = false;
            MethodInfo overlapSphere = AccessTools.Method(typeof(Physics), nameof(Physics.OverlapSphere), [typeof(Vector3), typeof(float), typeof(int), typeof(QueryTriggerInteraction)]),
                       gameNetworkManagerInstance = AccessTools.DeclaredPropertyGetter(typeof(GameNetworkManager), nameof(GameNetworkManager.Instance)),
                       setItemInElevator = AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.SetItemInElevator));
            FieldInfo localPlayerController = AccessTools.Field(typeof(GameNetworkManager), nameof(GameNetworkManager.localPlayerController));
            for (int i = 1; i < codes.Count - 7; i++)
            {
                if (codes[i].opcode == OpCodes.Call)
                {
                    MethodInfo methodInfo = codes[i].operand as MethodInfo;
                    if (!patchedTriggerInteraction && methodInfo == overlapSphere && codes[i - 1].opcode == OpCodes.Ldc_I4_1)
                    {
                        codes[i - 1].opcode = OpCodes.Ldc_I4_2;
                        Plugin.Logger.LogDebug("Transpiler (Cruiser collect): Auto-collect trigger colliders, for Teeth");
                        patchedTriggerInteraction = true;
                    }
                    else if (!creditLastDriver && methodInfo == gameNetworkManagerInstance && codes[i + 1].opcode == OpCodes.Ldfld && (FieldInfo)codes[i + 1].operand == localPlayerController && codes[i + 7].opcode == OpCodes.Callvirt && codes[i + 7].operand as MethodInfo == setItemInElevator)
                    {
                        codes[i].operand = AccessTools.Method(typeof(NonPatchFunctions), nameof(NonPatchFunctions.CruiserCreditsPlayer));
                        codes[i + 1].opcode = OpCodes.Nop;
                        Plugin.Logger.LogDebug("Transpiler (Cruiser collect): Credit last driver for collected scrap");
                        creditLastDriver = true;
                    }
                }
            }

            // reduce to a warning, because HighQuotaFixes includes a different solution for the teeth issue
            if (!patchedTriggerInteraction || !creditLastDriver)
                Plugin.Logger.LogWarning("Cruiser collect transpiler failed");
            return codes;
        }

        [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.Awake))]
        [HarmonyPostfix]
        static void VehicleController_Post_Awake(VehicleController __instance)
        {
            if (GlobalReferences.vehicleController == null)
                GlobalReferences.vehicleController = __instance;
        }

        [HarmonyPatch(typeof(BushWolfEnemy), nameof(BushWolfEnemy.Update))]
        [HarmonyPatch(typeof(CadaverBloomAI), nameof(CadaverBloomAI.GetPhysicsParentAtEnemyPosition))]
        [HarmonyPatch(typeof(ClipboardItem), nameof(ClipboardItem.Update))]
        [HarmonyPatch(typeof(ForestGiantAI), nameof(ForestGiantAI.OnCollideWithPlayer))]
        [HarmonyPatch(typeof(GiantKiwiAI), nameof(GiantKiwiAI.AttackIfThreatened))]
        [HarmonyPatch(typeof(GiantKiwiAI), nameof(GiantKiwiAI.CheckLOSForCreatures))]
        [HarmonyPatch(typeof(GiantKiwiAI), nameof(GiantKiwiAI.IsEggInsideClosedTruck))]
        [HarmonyPatch(typeof(GiantKiwiAI), nameof(GiantKiwiAI.NavigateTowardsTargetPlayer))]
        [HarmonyPatch(typeof(Landmine), nameof(Landmine.SpawnExplosion))]
        [HarmonyPatch(typeof(MouthDogAI), nameof(MouthDogAI.OnCollideWithPlayer))]
        //[HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.TrySprayingWeedKillerBottle))]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesClientRpc))]
        [HarmonyPatch(typeof(Terminal), nameof(Terminal.LoadNewNodeIfAffordable))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> CacheVehicleController(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && codes[i].operand as MethodInfo == ReflectionCache.FIND_OBJECT_OF_TYPE_VEHICLE_CONTROLLER)
                {
                    codes[i].opcode = OpCodes.Ldsfld;
                    codes[i].operand = ReflectionCache.VEHICLE_CONTROLLER;
                    Plugin.Logger.LogDebug($"Transpiler ({__originalMethod.DeclaringType}.{__originalMethod.Name}): Cache Cruiser script");
                }
            }

            //Plugin.Logger.LogWarning($"{__originalMethod.Name} transpiler failed");
            return codes;
        }

        [HarmonyPatch(nameof(VehicleController.Update))]
        [HarmonyPostfix]
        static void VehicleController_Post_Update(VehicleController __instance)
        {
            if (__instance.keyIsInDriverHand && __instance.vehicleID == 0 && __instance.currentDriver != null)
                CruiserAnimator.AnimateKeyForCruiser(__instance);
        }

        [HarmonyPatch(nameof(VehicleController.LateUpdate))]
        [HarmonyPostfix]
        static void VehicleController_Post_LateUpdate(VehicleController __instance)
        {
            if (__instance.currentDriver != null && GlobalReferences.lastDriver != __instance.currentDriver && !__instance.magnetedToShip)
                GlobalReferences.lastDriver = __instance.currentDriver;

            __instance.turboMeter.GetComponentInChildren<Renderer>().forceRenderingOff = __instance.carDestroyed || __instance.turboBoosts < 1;
        }

        [HarmonyPatch(nameof(VehicleController.StartMagneting))]
        [HarmonyPrefix]
        static bool VehicleController_Pre_StartMagneting(VehicleController __instance)
        {
            return !__instance.carDestroyed;
        }
    }
}
