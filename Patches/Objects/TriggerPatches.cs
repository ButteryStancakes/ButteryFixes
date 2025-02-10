using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch]
    internal class TriggerPatches
    {
        [HarmonyPatch(typeof(InteractTrigger), nameof(InteractTrigger.Interact))]
        [HarmonyPrefix]
        static void InteractTriggerPreInteract(InteractTrigger __instance, ref bool __state, bool ___hasTriggered)
        {
            __state = __instance.triggerOnce && !___hasTriggered && __instance.touchTrigger;
        }

        [HarmonyPatch(typeof(InteractTrigger), nameof(InteractTrigger.Interact))]
        [HarmonyPostfix]
        static void InteractTriggerPostInteract(InteractTrigger __instance, bool __state, Transform playerTransform, ref bool ___hasTriggered)
        {
            if (__state && ___hasTriggered && playerTransform.TryGetComponent(out PlayerControllerB player) && player.inVehicleAnimation)
                __instance.onInteract.Invoke(player);
        }
    }
}
