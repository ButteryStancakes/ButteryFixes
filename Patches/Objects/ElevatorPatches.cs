using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class ElevatorPatches
    {
        [HarmonyPatch(typeof(CadaverBloomAI), nameof(CadaverBloomAI.GetPhysicsParentAtEnemyPosition))]
        //[HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.IsInMineshaftStartRoomWithPlayer))]
        [HarmonyPatch(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.SetLineToExitFromRadarTarget))]
        //[HarmonyPatch(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.DoAIInterval))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> CacheMineshaftElevatorController(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = instructions.ToList();

            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Call && codes[i].operand as MethodInfo == ReflectionCache.FIND_OBJECT_OF_TYPE_MINESHAFT_ELEVATOR_CONTROLLER)
                {
                    codes[i].operand = ReflectionCache.ROUND_MANAGER_INSTANCE;
                    codes.Insert(i + 1, new(OpCodes.Ldfld, ReflectionCache.CURRENT_MINESHAFT_ELEVATOR));
                    Plugin.Logger.LogDebug($"Transpiler ({__originalMethod.DeclaringType}.{__originalMethod.Name}): Cache elevator script");
                    return codes;
                }
            }

            //Plugin.Logger.LogWarning($"{__originalMethod.Name} transpiler failed");
            return instructions;
        }
    }
}