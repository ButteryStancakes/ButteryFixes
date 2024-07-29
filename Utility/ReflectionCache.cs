using GameNetcodeStuff;
using HarmonyLib;
using System.Reflection;

namespace ButteryFixes.Utility
{
    internal class ReflectionCache
    {
        internal static readonly FieldInfo IS_IN_HANGAR_SHIP_ROOM = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isInHangarShipRoom));

        internal static readonly FieldInfo ENEMY_COLLIDERS = AccessTools.Field(typeof(ShotgunItem), "enemyColliders");

        internal static readonly FieldInfo SPAWN_PROBABILITIES = AccessTools.Field(typeof(RoundManager), "SpawnProbabilities");
        internal static readonly FieldInfo CURRENT_LEVEL = AccessTools.Field(typeof(RoundManager), nameof(RoundManager.currentLevel));

        internal static readonly MethodInfo SPAWN_PROBABILITIES_POST_PROCESS = AccessTools.Method(typeof(NonPatchFunctions), nameof(NonPatchFunctions.SpawnProbabilitiesPostProcess));
    }
}
