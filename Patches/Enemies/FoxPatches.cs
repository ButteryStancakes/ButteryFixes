using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(BushWolfEnemy))]
    static class FoxPatches
    {
        [HarmonyPatch(typeof(BushWolfEnemy), nameof(BushWolfEnemy.Update))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> BushWolfEnemy_Trans_Update(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            MethodInfo mathfMax = AccessTools.Method(typeof(Mathf), nameof(Mathf.Max), [typeof(int), typeof(int)]);
            MethodInfo mathfClamp = AccessTools.Method(typeof(Mathf), nameof(Mathf.Clamp), [typeof(int), typeof(int), typeof(int)]);
            FieldInfo livingPlayers = AccessTools.Field(typeof(StartOfRound), nameof(StartOfRound.livingPlayers));
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call)
                {
                    MethodInfo methodInfo = codes[i].operand as MethodInfo;
                    if (methodInfo == ReflectionCache.FIND_OBJECT_OF_TYPE_VEHICLE_CONTROLLER)
                    {
                        codes[i].opcode = OpCodes.Ldsfld;
                        codes[i].operand = ReflectionCache.VEHICLE_CONTROLLER;
                        Plugin.Logger.LogDebug($"Transpiler (BushWolfEnemy.Update): Cache Cruiser script");
                    }
                    else if (methodInfo == mathfMax && codes[i - 1].opcode == OpCodes.Ldc_I4_0 && codes[i - 4].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 4].operand == livingPlayers)
                    {
                        codes[i].operand = mathfClamp;
                        codes.Insert(i, new(OpCodes.Ldc_I4_3));
                        i++;
                        Plugin.Logger.LogDebug($"Transpiler (Fox AI): Clamp livingPlayers-1 between 0 and 3");
                    }
                }
            }

            return codes;
        }

        [HarmonyPatch(typeof(BushWolfEnemy), nameof(BushWolfEnemy.DoAIInterval))]
        [HarmonyPostfix]
        static void BushWolfEnemy_Post_DoAIInterval(BushWolfEnemy __instance)
        {
            if (__instance.checkPlayer > 3)
            {
                // checkPlayer can only be 4+ when a mod has resized the player array
                // very likely, players with resized lobbies will have empty slots leftover, which unnecessarily slows down the fox's target processing

                // 50 attempts to skip over empty player slots
                for (int i = 0; i < 50; i++)
                {
                    // a player is connected in this slot
                    if (__instance.checkPlayer < StartOfRound.Instance.allPlayerScripts.Length && StartOfRound.Instance.allPlayerScripts[__instance.checkPlayer] != null && (StartOfRound.Instance.allPlayerScripts[__instance.checkPlayer].isPlayerControlled || StartOfRound.Instance.allPlayerScripts[__instance.checkPlayer].isPlayerDead))
                        return;

                    // move to next slot
                    __instance.checkPlayer++;

                    // wraparound means we're done
                    if (__instance.checkPlayer >= StartOfRound.Instance.allPlayerScripts.Length)
                    {
                        __instance.checkPlayer = 0;
                        return;
                    }
                }
            }
        }
    }
}
