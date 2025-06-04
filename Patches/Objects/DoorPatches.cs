using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ButteryFixes.Patches.Objects
{
    [HarmonyPatch(typeof(DoorLock))]
    internal class DoorPatches
    {
        [HarmonyPatch(nameof(DoorLock.UnlockDoor))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> DoorLock_Trans_UnlockDoor(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            bool stopDone = false, playDone = false;
            MethodInfo stop = AccessTools.Method(typeof(AudioSource), nameof(AudioSource.Stop));
            MethodInfo playOneShot = AccessTools.Method(typeof(AudioSource), nameof(AudioSource.PlayOneShot), [typeof(AudioClip)]);
            FieldInfo doorLockSFX = AccessTools.Field(typeof(DoorLock), nameof(DoorLock.doorLockSFX));
            for (int i = 2; i < codes.Count; i++)
            {
                // only before first conditional
                if (codes[i].opcode == OpCodes.Brtrue || codes[i].opcode == OpCodes.Brtrue_S)
                    break;

                if (codes[i].opcode == OpCodes.Callvirt)
                {
                    MethodInfo method = codes[i].operand as MethodInfo;
                    // going to replace this with prefix/postfix
                    if (!stopDone && method == stop && codes[i - 1].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 1].operand == doorLockSFX)
                    {
                        codes.RemoveRange(i - 2, 3);
                        i -= 3;
                        stopDone = true;
                        Plugin.Logger.LogDebug("Transpiler (Door): Don't stop SFX");
                    }
                    else if (!playDone && method == playOneShot && i >= 4 && codes[i - 3].opcode == OpCodes.Ldfld && (FieldInfo)codes[i - 3].operand == doorLockSFX)
                    {
                        codes.RemoveRange(i - 4, 5);
                        i -= 5;
                        playDone = true;
                        Plugin.Logger.LogDebug("Transpiler (Door): Don't play unlock SFX");
                    }
                }
            }

            if (!stopDone || !playDone)
                Plugin.Logger.LogError("Door transpiler failed");

            return codes;
        }

        [HarmonyPatch(nameof(DoorLock.UnlockDoor))]
        [HarmonyPrefix]
        static void DoorLock_Pre_UnlockDoor(DoorLock __instance, ref bool __state)
        {
            __state = __instance.isLocked;
        }

        [HarmonyPatch(nameof(DoorLock.UnlockDoor))]
        [HarmonyPostfix]
        static void DoorLock_Post_UnlockDoor(DoorLock __instance, bool __state)
        {
            // door was previously locked, and only just became unlocked
            if (__state && !__instance.isLocked)
            {
                __instance.doorLockSFX.Stop();
                __instance.doorLockSFX.PlayOneShot(__instance.unlockSFX);
            }
        }
    }
}
