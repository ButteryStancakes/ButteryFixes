using System.Reflection;

namespace ButteryFixes.Utility
{
    internal class PrivateMembers
    {
        internal static readonly FieldInfo JETPACK_ACTIVATED = typeof(JetpackItem).GetField("jetpackActivated", BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly FieldInfo JETPACK_ITEM_PREVIOUS_PLAYER_HELD_BY = typeof(JetpackItem).GetField("previousPlayerHeldBy", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static readonly MethodInfo GLOBAL_NUTCRACKER_CLOCK = typeof(NutcrackerEnemyAI).GetMethod("GlobalNutcrackerClock", BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly FieldInfo IS_LEADER_SCRIPT = typeof(NutcrackerEnemyAI).GetField("isLeaderScript", BindingFlags.Instance | BindingFlags.NonPublic);
    }
}
