using HarmonyLib;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ButteryFixes.Patches
{
    [HarmonyPatch]
    internal class GeneralPatches
    {
        internal static float MUSIC_DOPPLER_LEVEL = 1f;

        [HarmonyPatch(typeof(QuickMenuManager), "Start")]
        [HarmonyPostfix]
        static void QuickMenuManagerPostStart(QuickMenuManager __instance)
        {
            SpawnableEnemyWithRarity oldBird = __instance.testAllEnemiesLevel.OutsideEnemies.FirstOrDefault(enemy => enemy.enemyType.name == "RadMech");
            if (oldBird != null)
                oldBird.enemyType.requireNestObjectsToSpawn = true;
            Plugin.Logger.LogInfo("Old Birds now require \"nest\" to spawn");
            SpawnableEnemyWithRarity masked = __instance.testAllEnemiesLevel.Enemies.FirstOrDefault(enemy => enemy.enemyType.name == "MaskedPlayerEnemy");
            if (masked != null)
                masked.enemyType.isOutsideEnemy = false;
            Plugin.Logger.LogInfo("\"Masked\" now subtract from indoor power level");
            EnemyPatches.FLOWER_SNAKE = __instance.testAllEnemiesLevel.DaytimeEnemies.FirstOrDefault(enemy => enemy.enemyType.name == "FlowerSnake")?.enemyType;
        }

        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        static void StartOfRoundPostAwake(StartOfRound __instance)
        {
            SelectableLevel rend = __instance.levels.FirstOrDefault(level => level.name == "RendLevel");
            if (rend != null)
            {
                for (int i = 0; i < rend.spawnableMapObjects.Length; i++)
                {
                    if (rend.spawnableMapObjects[i].prefabToSpawn != null && rend.spawnableMapObjects[i].prefabToSpawn.name == "SpikeRoofTrapHazard")
                    {
                        rend.spawnableMapObjects[i].requireDistanceBetweenSpawns = true;
                        Plugin.Logger.LogInfo("Rend now properly spaces spike traps");
                    }
                }
            }

            GameObject helmetVisor = GameObject.Find("/Systems/Rendering/PlayerHUDHelmetModel/ScavengerHelmet");
            if (helmetVisor != null)
            {
                helmetVisor.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                Plugin.Logger.LogInfo("\"Fake helmet\" no longer casts a shadow");
            }

            switch (Plugin.configMusicDopplerLevel.Value)
            {
                case MusicDopplerLevel.None:
                    MUSIC_DOPPLER_LEVEL = 0f;
                    break;
                case MusicDopplerLevel.Reduced:
                    MUSIC_DOPPLER_LEVEL = 0.3f;
                    break;
                default:
                    MUSIC_DOPPLER_LEVEL = 1f;
                    break;
            }

            __instance.speakerAudioSource.dopplerLevel = MUSIC_DOPPLER_LEVEL;
            Plugin.Logger.LogInfo("Doppler level: Ship speaker");

            foreach (Item item in __instance.allItemsList.itemsList)
            {

                switch (item.name)
                {
                    case "Knife":
                        item.spawnPrefab.GetComponent<KnifeItem>().SetScrapValue(35);
                        Plugin.Logger.LogInfo("Kitchen knives now display value on scan");
                        break;
                    case "Boombox":
                        item.spawnPrefab.GetComponent<BoomboxItem>().boomboxAudio.dopplerLevel = 0.3f * MUSIC_DOPPLER_LEVEL;
                        Plugin.Logger.LogInfo("Doppler level: Boombox");
                        break;
                }
            }

            foreach (UnlockableItem unlockableItem in __instance.unlockablesList.unlockables)
            {
                switch (unlockableItem.unlockableName)
                {
                    /*case "Television":
                        unlockableItem.prefabObject.GetComponentInChildren<TVScript>().tvSFX.dopplerLevel = MUSIC_DOPPLER_LEVEL;
                        Plugin.Logger.LogInfo("Doppler level: Television");
                        break;*/
                    case "Record player":
                        unlockableItem.prefabObject.GetComponentInChildren<AnimatedObjectTrigger>().thisAudioSource.dopplerLevel = MUSIC_DOPPLER_LEVEL;
                        Plugin.Logger.LogInfo("Doppler level: Record player");
                        break;
                    case "Disco Ball":
                        unlockableItem.prefabObject.GetComponentInChildren<CozyLights>().turnOnAudio.dopplerLevel = 0.92f * MUSIC_DOPPLER_LEVEL;
                        Plugin.Logger.LogInfo("Doppler level: Disco ball");
                        break;
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetStats))]
        [HarmonyPostfix]
        static void StartOfRoundPostResetStats(StartOfRound __instance)
        {
            for (int i = 0; i < __instance.gameStats.allPlayerStats.Length; i++)
                __instance.gameStats.allPlayerStats[i].profitable = 0;
            Plugin.Logger.LogInfo("Cleared \"profitable\" stat for all employees");
        }

        [HarmonyPatch(typeof(StartOfRound), "ResetShipFurniture")]
        [HarmonyPostfix]
        static void PostResetShipFurniture(StartOfRound __instance)
        {
            if (__instance.IsServer)
            {
                Terminal terminal = Object.FindObjectOfType<Terminal>();
                if (terminal != null && terminal.orderedItemsFromTerminal.Count > 0)
                {
                    terminal.orderedItemsFromTerminal.Clear();
                    terminal.SyncGroupCreditsServerRpc(terminal.groupCredits, 0);
                    Plugin.Logger.LogInfo("Dropship inventory was emptied (game over)");
                }
            }
        }

        [HarmonyPatch(typeof(ItemDropship), "Start")]
        [HarmonyPostfix]
        static void ItemDropshipPostStart(ItemDropship __instance)
        {
            Transform music = __instance.transform.Find("Music");
            music.GetComponent<AudioSource>().dopplerLevel = 0.6f * MUSIC_DOPPLER_LEVEL;
            music.Find("Music (1)").GetComponent<AudioSource>().dopplerLevel = 0.6f * MUSIC_DOPPLER_LEVEL;
            Plugin.Logger.LogInfo("Doppler level: Dropship");
        }
    }
}
