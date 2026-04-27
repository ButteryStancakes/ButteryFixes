using GameNetcodeStuff;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace ButteryFixes.Utility
{
    internal static class ReflectionCache
    {
        internal static readonly FieldInfo IS_IN_HANGAR_SHIP_ROOM = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isInHangarShipRoom));

        internal static readonly MethodInfo FIND_OBJECT_OF_TYPE_VEHICLE_CONTROLLER = AccessTools.Method(typeof(Object), nameof(Object.FindObjectOfType), null, [typeof(VehicleController)]);
        internal static readonly MethodInfo FIND_OBJECT_OF_TYPE_MOLD_SPREAD_MANAGER = AccessTools.Method(typeof(Object), nameof(Object.FindObjectOfType), null, [typeof(MoldSpreadManager)]);
        internal static readonly FieldInfo VEHICLE_CONTROLLER = AccessTools.Field(typeof(GlobalReferences), nameof(GlobalReferences.vehicleController));
        internal static readonly MethodInfo MOLD_SPREAD_MANAGER = AccessTools.DeclaredPropertyGetter(typeof(GlobalReferences), nameof(GlobalReferences.MoldSpreadManager));

        internal static readonly MethodInfo SET_SCRAP_VALUE = AccessTools.Method(typeof(GrabbableObject), nameof(GrabbableObject.SetScrapValue));
        internal static readonly MethodInfo TRACK_GIFT_BOX_ON_CLIENT = AccessTools.Method(typeof(ScrapTracker), nameof(ScrapTracker.TrackGiftBoxOnClient));
    }
}
