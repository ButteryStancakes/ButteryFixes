using HarmonyLib;
using UnityEngine;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(TimeOfDay))]
    static class TimeOfDayPatches
    {
        [HarmonyPatch(nameof(TimeOfDay.SetWeatherBasedOnVariables))]
        [HarmonyPrefix]
        static void TimeOfDay_Pre_SetWeatherBasedOnVariables(TimeOfDay __instance, ref bool __state)
        {
            __state = __instance.placedFloodNavMesh;
        }

        [HarmonyPatch(nameof(TimeOfDay.SetWeatherBasedOnVariables))]
        [HarmonyPostfix]
        static void TimeOfDay_Post_SetWeatherBasedOnVariables(TimeOfDay __instance, RandomWeatherWithVariables weatherVariables, bool __state)
        {
            if (__instance.placedFloodNavMesh && !__state)
            {
                GameObject outsideLevelNavMesh = GameObject.FindGameObjectWithTag("OutsideLevelNavMesh");
                if (outsideLevelNavMesh != null)
                {
                    Transform floodWeatherNavArea = outsideLevelNavMesh.transform.Find("FloodWeatherNavArea(Clone)");
                    if (floodWeatherNavArea != null)
                    {
                        float y = floodWeatherNavArea.transform.position.y;
                        floodWeatherNavArea.transform.position = new(24.7886009f, Mathf.Lerp(weatherVariables.weatherVariable, weatherVariables.weatherVariable + weatherVariables.weatherVariable2, 0.5f), 5.09009981f);
                        Plugin.Logger.LogDebug($"Adjusted offset of flooded weather navmesh modifier: {y} -> {floodWeatherNavArea.transform.position.y}");
                    }
                }
            }
        }

        [HarmonyPatch(nameof(TimeOfDay.Awake))]
        [HarmonyPostfix]
        static void TimeOfDay_Post_Awake(TimeOfDay __instance)
        {
            foreach (ParticleSystem particleSystem in __instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.CollisionModule collisionModule = particleSystem.collision;
                if (collisionModule.enabled && particleSystem.GetComponent<ParticleSystemRenderer>().sharedMaterial.name.StartsWith("RainParticle"))
                {
                    collisionModule.collidesWith |= 1 << 30;
                    Plugin.Logger.LogDebug($"Rain particles \"{particleSystem.name}\" collide with vehicles");
                }
            }
        }
    }
}
