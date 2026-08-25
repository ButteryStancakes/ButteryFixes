using ButteryFixes.Utility;
using HarmonyLib;
using System;
using UnityEngine;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch(typeof(FlowerSnakeEnemy))]
    static class TulipPatches
    {
        [HarmonyPatch(nameof(FlowerSnakeEnemy.Start))]
        [HarmonyPostfix]
        static void FlowerSnakeEnemy_Post_Start(FlowerSnakeEnemy __instance)
        {
            if (GlobalReferences.sphere != null && GlobalReferences.pumpkinMatPlastic != null && DateTime.Today == new DateTime(DateTime.Now.Year, 10, 31))
            {
                Transform chest001 = __instance.creatureAnimator?.transform.Find("Armature/Belly/LowerChest/Chest/Chest.001");
                if (chest001 != null)
                {
                    GameObject sphere = new("Sphere");
                    sphere.transform.SetParent(chest001);
                    sphere.transform.SetLocalPositionAndRotation(new(-0.00712880259f, 1.03152323f, 0.279703677f), Quaternion.Euler(-4.87f, 0f, 90f));
                    sphere.transform.localScale = new(1.88127434f, 1.88127351f, 1.88127375f);
                    sphere.AddComponent<MeshFilter>().sharedMesh = GlobalReferences.sphere;
                    sphere.AddComponent<MeshRenderer>().sharedMaterial = GlobalReferences.pumpkinMatPlastic;

                    Plugin.Logger.LogDebug($"Spooky snake #{__instance.GetInstanceID()}");
                }
            }
        }
    }
}
