using ButteryFixes.Utility;
using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(MicrowaveItem))]
    internal class MicrowavePatches
    {
        [HarmonyPatch(nameof(MicrowaveItem.TurnOnMicrowave))]
        [HarmonyPrefix]
        static bool MicrowaveItem_Pre_TurnOnMicrowave(MicrowaveItem __instance, bool on)
        {
            NonPatchFunctions.ClearMicrowave();
            if (__instance.microwaveOnDelay != null)
            {
                __instance.StopCoroutine(__instance.microwaveOnDelay);
                __instance.microwaveOnDelay = null;
            }

            if (!on)
            {
                __instance.whirringAudio.PlayOneShot(__instance.microwaveClose);
                __instance.microwaveOnDelay = __instance.StartCoroutine(startMicrowaveOnDelay(__instance));
            }
            else
            {
                __instance.whirringAudio.Stop();
                __instance.whirringAudio.PlayOneShot(__instance.microwaveOpen);
            }

            return false;
        }

        static IEnumerator startMicrowaveOnDelay(MicrowaveItem microwaveItem)
        {
            yield return new WaitForSeconds(0.25f);
            RoundManager.Instance.PlayAudibleNoise(microwaveItem.mainObject.transform.position, 8f, 0.6f, 0, StartOfRound.Instance.hangarDoorsClosed, 0);
            yield return new WaitForSeconds(0.2857143f); // 0.5f
            microwaveItem.whirringAudio.Play();

            // new
            GrabbableObject[] allChildrenItems = microwaveItem.mainObject.GetComponentsInChildren<GrabbableObject>();
            foreach (GrabbableObject item in allChildrenItems)
            {
                item.rotateObject = true;
                GlobalReferences.microwavedItems.Add(item);
            }

        }
    }
}
