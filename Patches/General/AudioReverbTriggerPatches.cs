using ButteryFixes.Utility;
using GameNetcodeStuff;
using HarmonyLib;

namespace ButteryFixes.Patches.General
{
    [HarmonyPatch(typeof(AudioReverbTrigger))]
    internal class AudioReverbTriggerPatches
    {
        [HarmonyPatch(nameof(AudioReverbTrigger.ChangeAudioReverbForPlayer))]
        [HarmonyPostfix]
        static void AudioReverbTrigger_Post_ChangeAudioReverbForPlayer(AudioReverbTrigger __instance)
        {
            if (GameNetworkManager.Instance?.localPlayerController == null || __instance.playerScript == null || !__instance.playerScript.isPlayerControlled)
                return;

            if (StartOfRound.Instance == null || StartOfRound.Instance.inShipPhase || !StartOfRound.Instance.shipDoorsEnabled || StartOfRound.Instance.suckingPlayersOutOfShip)
                return;

            if (__instance.elevatorTriggerForProps && Configuration.autoCollect.Value)
            {
                // wait until player state matches this trigger
                if (__instance.playerScript.isInHangarShipRoom != __instance.isShipRoom || __instance.playerScript.isInElevator != __instance.setInElevatorTrigger)
                    return;

                for (int i = 0; i < __instance.playerScript.ItemSlots.Length; i++)
                {
                    if (__instance.playerScript.ItemSlots[i] == null)
                        continue;

                    // enforce state for all inventory items, not just the one in their hand
                    if (__instance.playerScript.ItemSlots[i].isInShipRoom != __instance.playerScript.isInHangarShipRoom || __instance.playerScript.ItemSlots[i].isInElevator != __instance.playerScript.isInElevator)
                    {
                        __instance.playerScript.SetAllItemsInElevator(__instance.playerScript.isInHangarShipRoom, __instance.playerScript.isInElevator);
                        break;
                    }
                }
            }
        }
    }
}
