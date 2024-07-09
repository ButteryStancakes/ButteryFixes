using GameNetcodeStuff;
using HarmonyLib;
using System.Reflection;

namespace ButteryFixes.Utility
{
    internal class ReflectionCache
    {
        //internal static readonly FieldInfo JETPACK_ITEM_PREVIOUS_PLAYER_HELD_BY = AccessTools.Field(typeof(JetpackItem), "previousPlayerHeldBy");
        //internal static readonly FieldInfo JETPACK_BROKEN = AccessTools.Field(typeof(JetpackItem), "jetpackBroken");

        internal static readonly MethodInfo GLOBAL_NUTCRACKER_CLOCK = AccessTools.Method(typeof(NutcrackerEnemyAI), "GlobalNutcrackerClock");
        internal static readonly FieldInfo IS_LEADER_SCRIPT = AccessTools.Field(typeof(NutcrackerEnemyAI), "isLeaderScript");

        internal static readonly FieldInfo IS_IN_HANGAR_SHIP_ROOM = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isInHangarShipRoom));

        internal static readonly FieldInfo ENEMY_COLLIDERS = AccessTools.Field(typeof(ShotgunItem), "enemyColliders");
    }
}
