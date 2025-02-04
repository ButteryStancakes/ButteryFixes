using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    internal class AIPatches
    {
        [HarmonyPatch(typeof(EnemyAI), "SubtractFromPowerLevel")]
        [HarmonyPrefix]
        static void PreSubtractFromPowerLevel(EnemyAI __instance, ref bool ___removedPowerLevel)
        {
            if (___removedPowerLevel)
                return;

            if (__instance is ButlerEnemyAI && Configuration.maskHornetsPower.Value)
            {
                Plugin.Logger.LogDebug("Butler died, but mask hornets don't decrease power level");
                ___removedPowerLevel = true;
            }
        }

        [HarmonyPatch(typeof(DoorLock), "OnTriggerStay")]
        [HarmonyPrefix]
        public static bool DoorLockPreOnTriggerStay(Collider other)
        {
            // snare fleas, tulip snakes, and maneater don't open door when latching to player
            return !(other.CompareTag("Enemy") && other.TryGetComponent(out EnemyAICollisionDetect enemyAICollisionDetect) && (enemyAICollisionDetect.mainScript is CentipedeAI { clingingToPlayer: not null } || enemyAICollisionDetect.mainScript is FlowerSnakeEnemy { clingingToPlayer: not null } || enemyAICollisionDetect.mainScript is CaveDwellerAI { propScript.playerHeldBy: not null }));
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.KillEnemyOnOwnerClient))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> TransKillEnemyOnOwnerClient(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            FieldInfo destroyOnDeath = AccessTools.Field(typeof(EnemyType), nameof(EnemyType.destroyOnDeath));
            for (int i = 1; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Stloc_0 && codes[i - 1].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 1].operand == destroyOnDeath)
                {
                    codes.InsertRange(i, [
                        new(OpCodes.Ldarg_1),
                        new(OpCodes.Or)
                    ]);
                    Plugin.Logger.LogDebug("Transpiler (Enemy kill): Allow unkillable enemies to be destroyed by Earth Leviathan");
                    return codes; // i += 2;
                }
            }

            Plugin.Logger.LogError("Enemy kill transpiler failed");
            return instructions;
        }
    }
}
