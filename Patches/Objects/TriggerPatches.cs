using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(InteractTrigger))]
    internal class TriggerPatches
    {
        [HarmonyPatch(nameof(InteractTrigger.Interact))]
        [HarmonyPrefix]
        static void InteractTrigger_Pre_Interact(InteractTrigger __instance, ref bool __state)
        {
            __state = __instance.triggerOnce && !__instance.hasTriggered && __instance.touchTrigger;
        }

        [HarmonyPatch(nameof(InteractTrigger.Interact))]
        [HarmonyPostfix]
        static void InteractTrigger_Post_Interact(InteractTrigger __instance, bool __state, Transform playerTransform)
        {
            if (__state && __instance.hasTriggered && playerTransform.TryGetComponent(out PlayerControllerB player) && player.inVehicleAnimation)
                __instance.onInteract.Invoke(player);
        }
    }
}
