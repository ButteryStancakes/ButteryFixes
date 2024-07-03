using ButteryFixes.Utility;
using HarmonyLib;

namespace ButteryFixes.Patches.Enemies
{
    [HarmonyPatch]
    internal class TulipSnakePatches
    {
        [HarmonyPatch(typeof(FlowerSnakeEnemy), "SetFlappingLocalClient")]
        [HarmonyPostfix]
        public static void PostSetFlappingLocalClient(FlowerSnakeEnemy __instance, bool isMainSnake/*, bool setFlapping*/)
        {
            // if the current snake is dropping a player
            if (!isMainSnake /*|| setFlapping*/ || __instance.clingingToPlayer != GameNetworkManager.Instance.localPlayerController || !__instance.clingingToPlayer.disablingJetpackControls)
                return;

            for (int i = 0; i < __instance.clingingToPlayer.ItemSlots.Length; i++)
            {
                // if the item is equipped
                if (__instance.clingingToPlayer.ItemSlots[i] == null || __instance.clingingToPlayer.ItemSlots[i].isPocketed)
                    continue;

                if (__instance.clingingToPlayer.ItemSlots[i] is JetpackItem)
                {
                    // and is a jetpack that's activated
                    JetpackItem heldJetpack = __instance.clingingToPlayer.ItemSlots[i] as JetpackItem;
                    if ((bool)ReflectionCache.JETPACK_ACTIVATED.GetValue(heldJetpack))
                    {
                        __instance.clingingToPlayer.disablingJetpackControls = false;
                        __instance.clingingToPlayer.maxJetpackAngle = -1f;
                        __instance.clingingToPlayer.jetpackRandomIntensity = 0f;
                        Plugin.Logger.LogInfo("Player still using jetpack when tulip snake dropped; re-enable flight controls");
                        return;
                    }
                }
            }
        }
    }
}
