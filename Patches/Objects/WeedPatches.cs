using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(MoldSpreadManager))]
    static class WeedPatches
    {
        [HarmonyPatch(nameof(MoldSpreadManager.GetBiggestWeedPatch))]
        [HarmonyPostfix]
        static void MoldSpreadManager_Post_GetBiggestWeedPatch(MoldSpreadManager __instance)
        {
            // means this function ran
            if (__instance.mostSurroundingSpores != 0)
            {
                if (GameObject.FindGameObjectWithTag("MoldAttractionPoint") == null)
                {
                    __instance.aggressivePosition = __instance.mostHiddenPosition;
                    Plugin.Logger.LogInfo("Found no aggressive position reason D");
                }
            }
        }

        [HarmonyPatch(typeof(BushWolfEnemy), nameof(BushWolfEnemy.Update))]
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.SaveGameValues))]
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GenerateNewLevelClientRpc))]
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.LoadNewLevelWait), MethodType.Enumerator)]
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.ResetEnemyVariables))]
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnWeedEnemies))]
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnRandomOutsideEnemy))]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndOfGame), MethodType.Enumerator)]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetMoldStates))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> CacheWeedScript(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call)
                {
                    string methodName = codes[i].operand.ToString();
                    if (methodName.Contains("FindObjectOfType") && methodName.Contains("MoldSpreadManager"))
                    {
                        codes[i].operand = ReflectionCache.MOLD_SPREAD_MANAGER;
                        Plugin.Logger.LogDebug($"Transpiler ({__originalMethod.DeclaringType}.{__originalMethod.Name}): Cache mold manager");
                    }
                }
            }

            //Plugin.Logger.LogWarning($"{__originalMethod.Name} transpiler failed");
            return codes;
        }
    }
}
