using GameNetcodeStuff;
using HarmonyLib;
using System.Reflection;

namespace ButteryFixes.Utility
{
    internal static class ReflectionCache
    {
        internal static readonly FieldInfo IS_IN_HANGAR_SHIP_ROOM = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isInHangarShipRoom));

        internal static FieldInfo VEHICLE_CONTROLLER = AccessTools.Field(typeof(GlobalReferences), nameof(GlobalReferences.vehicleController));
        internal static MethodInfo MOLD_SPREAD_MANAGER = AccessTools.DeclaredPropertyGetter(typeof(GlobalReferences), nameof(GlobalReferences.MoldSpreadManager));
    }
}
