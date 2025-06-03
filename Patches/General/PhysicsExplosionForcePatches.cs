using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(PhysicsExplosionForce))]
    class PhysicsExplosionForcePatches
    {
        [HarmonyPatch(nameof(PhysicsExplosionForce.Start))]
        [HarmonyPrefix]
        static bool PhysicsExplosionForce_Pre_Start(PhysicsExplosionForce __instance)
        {
            if (__instance.name.StartsWith("MeatChunk"))
            {
                __instance.enabled = false;
                return false;
            }

            __instance.bodyParts = __instance.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody bodyPart in __instance.bodyParts)
                bodyPart.AddExplosionForce(100f, __instance.transform.position, 0f, 3.5f, ForceMode.Impulse);

            return false;
        }

        [HarmonyPatch(nameof(PhysicsExplosionForce.Update))]
        [HarmonyPrefix]
        static bool PhysicsExplosionForce_Pre_Update(PhysicsExplosionForce __instance)
        {
            for (int i = 0; i < __instance.bodyParts.Length; i++)
            {
                if (__instance.bodyParts[i] != null && __instance.bodyParts[i].transform.position.y < -200f)
                    Object.Destroy(__instance.bodyParts[i].gameObject);
            }

            return false;
        }
    }
}
