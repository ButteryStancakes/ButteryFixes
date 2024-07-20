using ButteryFixes.Utility;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch]
    internal class RoundManagerPatches
    {
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.PredictAllOutsideEnemies))]
        [HarmonyPostfix]
        static void PostPredictAllOutsideEnemies()
        {
            // cleans up leftover spawn numbers from previous day + spawn predictions (when generating nests)
            foreach (string name in GlobalReferences.allEnemiesList.Keys)
                GlobalReferences.allEnemiesList[name].numberSpawned = 0;
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.OnDestroy))]
        [HarmonyPostfix]
        static void RoundManagerPostOnDestroy(RoundManager __instance)
        {
            // prevents persistence when quitting mid-day and rehosting
            __instance.ResetEnemyVariables();
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.PowerSwitchOffClientRpc))]
        [HarmonyPostfix]
        static void PostPowerSwitchOffClientRpc()
        {
            Object.FindObjectOfType<BreakerBox>()?.breakerBoxHum.Stop();
        }

        [HarmonyPatch(typeof(RoundManager), "SetExitIDs")]
        [HarmonyPostfix]
        static void PostSetExitIDs(RoundManager __instance)
        {
            if (Configuration.fixFireExits.Value)
            {
                foreach (EntranceTeleport entranceTeleport in Object.FindObjectsOfType<EntranceTeleport>())
                {
                    if (entranceTeleport.entranceId > 0 && !entranceTeleport.isEntranceToBuilding)
                    {
                        entranceTeleport.entrancePoint.localRotation = Quaternion.Euler(entranceTeleport.entrancePoint.localEulerAngles.x, entranceTeleport.entrancePoint.localEulerAngles.y + 180f, entranceTeleport.entrancePoint.localEulerAngles.z);
                        Plugin.Logger.LogInfo("Fixed rotation of internal fire exit");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnMapObjects))]
        [HarmonyPostfix]
        static void PostSpawnMapObjects(RoundManager __instance)
        {
            if (StartOfRound.Instance.currentLevel.name == "RendLevel" || StartOfRound.Instance.currentLevel.name == "DineLevel" || StartOfRound.Instance.currentLevel.name == "TitanLevel")
            {
                if (__instance.mapPropsContainer != null)
                {
                    bool retex = false;
                    foreach (Transform mapProp in __instance.mapPropsContainer.transform)
                    {
                        if (mapProp.name.StartsWith("LargeRock"))
                        {
                            foreach (Renderer rend in mapProp.GetComponentsInChildren<Renderer>())
                            {
                                rend.material.SetTexture("_MainTex", null);
                                rend.material.SetTexture("_BaseColorMap", null);
                                retex = true;
                            }
                        }
                    }
                    if (retex)
                        Plugin.Logger.LogInfo($"Skinned boulders for snowy moon \"{StartOfRound.Instance.currentLevel.name}\"");
                }
            }
        }
    }
}
