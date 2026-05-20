using GameNetcodeStuff;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace ButteryFixes.Utility
{
    internal static class ReflectionCache
    {
        internal static readonly FieldInfo  IS_IN_HANGAR_SHIP_ROOM = AccessTools.Field(typeof(PlayerControllerB), nameof(PlayerControllerB.isInHangarShipRoom)),

                                            VEHICLE_CONTROLLER = AccessTools.Field(typeof(GlobalReferences), nameof(GlobalReferences.vehicleController)),

                                            CURRENT_MINESHAFT_ELEVATOR = AccessTools.Field(typeof(RoundManager), nameof(RoundManager.currentMineshaftElevator)),

                                            CADAVER_GROWTH_AI = AccessTools.Field(typeof(GlobalReferences), nameof(GlobalReferences.cadaverGrowthAI));

        internal static readonly MethodInfo FIND_OBJECT_OF_TYPE_VEHICLE_CONTROLLER = AccessTools.Method(typeof(Object), nameof(Object.FindObjectOfType), null, [typeof(VehicleController)]),
                                            FIND_OBJECT_OF_TYPE_MOLD_SPREAD_MANAGER = AccessTools.Method(typeof(Object), nameof(Object.FindObjectOfType), null, [typeof(MoldSpreadManager)]),
                                            FIND_OBJECT_OF_TYPE_MINESHAFT_ELEVATOR_CONTROLLER = AccessTools.Method(typeof(Object), nameof(Object.FindObjectOfType), null, [typeof(MineshaftElevatorController)]),
                                            FIND_OBJECT_OF_TYPE_CADAVER_GROWTH_AI = AccessTools.Method(typeof(Object), nameof(Object.FindObjectOfType), null, [typeof(CadaverGrowthAI)]),
                                            MOLD_SPREAD_MANAGER = AccessTools.DeclaredPropertyGetter(typeof(GlobalReferences), nameof(GlobalReferences.MoldSpreadManager)),
            
                                            SET_SCRAP_VALUE = AccessTools.Method(typeof(GrabbableObject), nameof(GrabbableObject.SetScrapValue)),
                                            TRACK_GIFT_BOX_ON_CLIENT = AccessTools.Method(typeof(ScrapTracker), nameof(ScrapTracker.TrackGiftBoxOnClient)),

                                            ROUND_MANAGER_INSTANCE = AccessTools.DeclaredPropertyGetter(typeof(RoundManager), nameof(RoundManager.Instance));
    }
}
