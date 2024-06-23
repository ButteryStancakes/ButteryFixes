using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ButteryFixes.Utility
{
    internal class ScriptableObjectOverrides
    {
        internal static void OverrideEnemyTypes()
        {
            foreach (KeyValuePair<string, EnemyType> enemy in GlobalReferences.allEnemiesList)
            {
                switch (enemy.Key)
                {
                    case "RadMech":
                        enemy.Value.requireNestObjectsToSpawn = true;
                        Plugin.Logger.LogInfo("Old Birds now require \"nest\" to spawn");
                        break;
                    case "MaskedPlayerEnemy":
                        enemy.Value.isOutsideEnemy = false;
                        Plugin.Logger.LogInfo("\"Masked\" now subtract from indoor power level");
                        break;
                    case "Blob":
                        enemy.Value.canDie = false;
                        Plugin.Logger.LogInfo("Hygroderes won't \"die\" when crushed by spike trap");
                        break;
                }
                // fix residue in ScriptableObject
                enemy.Value.numberSpawned = 0;
            }
        }

        internal static void OverrideSelectableLevels()
        {
            foreach (SelectableLevel selectableLevel in StartOfRound.Instance.levels)
            {
                switch (selectableLevel.name)
                {
                    case "RendLevel":
                        SpawnableMapObject spikeRoofTrapHazard = selectableLevel.spawnableMapObjects.FirstOrDefault(spawnableMapObject => spawnableMapObject.prefabToSpawn?.name == "SpikeRoofTrapHazard");
                        if (spikeRoofTrapHazard != null)
                        {
                            spikeRoofTrapHazard.requireDistanceBetweenSpawns = true;
                            Plugin.Logger.LogInfo("Rend now properly spaces spike traps");
                        }
                        break;
                }
            }
        }

        internal static void OverrideItems()
        {
            Dictionary<string, bool> conductiveItems = new()
            {
                //{ "Airhorn", true },
                //{ "DustPan", true },
                { "FancyCup", true },
                { "Flask", false },
                //{ "Hairdryer", true },
                { "MoldPan", true },
                //{ "Phone", true },
                { "Shotgun", true },
                { "SprayPaint", true },
                //{ "SteeringWheel", true }
            };

            foreach (Item item in StartOfRound.Instance.allItemsList.itemsList)
            {
                if (item == null)
                {
                    Plugin.Logger.LogWarning("Encountered a missing item in StartOfRound.allItemsList; this is probably an issue with another mod");
                    continue;
                }

                switch (item.name)
                {
                    case "Boombox":
                        item.spawnPrefab.GetComponent<BoomboxItem>().boomboxAudio.dopplerLevel = 0.3f * GlobalReferences.dopplerLevelMult;
                        Plugin.Logger.LogInfo("Doppler level: Boombox");
                        break;
                    case "Cog1":
                    case "EasterEgg":
                    case "FishTestProp":
                    case "MapDevice":
                    case "RedLocustHive":
                    case "ZapGun":
                        item.spawnPrefab.GetComponent<AudioSource>().rolloffMode = AudioRolloffMode.Linear;
                        Plugin.Logger.LogInfo($"Audio rolloff: {item.itemName}");
                        break;
                    case "ExtensionLadder":
                    case "RadarBooster":
                        item.canBeInspected = false;
                        Plugin.Logger.LogInfo($"Inspectable: {item.itemName} (False)");
                        break;
                    case "Key":
                        if (Plugin.configKeysAreScrap.Value)
                        {
                            item.isScrap = true;
                            Plugin.Logger.LogInfo("Scrap: Key");
                        }
                        else
                        {
                            item.spawnPrefab.GetComponent<KeyItem>().scrapValue = 0;
                            ScanNodeProperties scanNodeProperties = item.spawnPrefab.GetComponentInChildren<ScanNodeProperties>();
                            if (scanNodeProperties != null)
                            {
                                scanNodeProperties.subText = string.Empty;
                                Plugin.Logger.LogInfo("Scan node: Key");
                            }
                        }
                        break;
                    case "Knife":
                        KnifeItem knifeItem = item.spawnPrefab.GetComponent<KnifeItem>();
                        knifeItem.SetScrapValue(knifeItem.scrapValue);
                        Plugin.Logger.LogInfo("Scan node: Kitchen knife");
                        break;
                    case "MagnifyingGlass":
                    case "PillBottle":
                        item.canBeInspected = true;
                        Plugin.Logger.LogInfo($"Inspectable: {item.itemName} (True)");
                        break;
                    case "TragedyMask":
                        GlobalReferences.tragedyMaskRandomClips = item.spawnPrefab.GetComponent<RandomPeriodicAudioPlayer>()?.randomClips;
                        MeshFilter maskMesh = item.spawnPrefab.transform.Find("MaskMesh")?.GetComponent<MeshFilter>();
                        if (maskMesh != null)
                        {
                            MeshFilter eyesFilled = maskMesh.transform.Find("EyesFilled")?.GetComponent<MeshFilter>();
                            if (eyesFilled != null && GlobalReferences.tragedyMaskEyesFilled != null)
                            {
                                eyesFilled.mesh = GlobalReferences.tragedyMaskEyesFilled;
                                MeshFilter maskLOD = maskMesh.transform.Find("ComedyMaskLOD")?.GetComponent<MeshFilter>();
                                if (maskLOD != null && GlobalReferences.tragedyMaskLOD != null)
                                {
                                    maskLOD.mesh = GlobalReferences.tragedyMaskLOD;
                                    Plugin.Logger.LogInfo("Meshes: Tragedy");
                                }
                            }
                        }
                        break;
                }

                // affects whoopie cushion primarily
                if (item.spawnPrefab != null)
                {
                    bool triggerHidden = false;
                    foreach (Renderer rend in item.spawnPrefab.GetComponentsInChildren<Renderer>())
                    {
                        if (rend.enabled && rend.gameObject.layer == 13)
                        {
                            rend.enabled = false;
                            triggerHidden = true;
                        }
                    }
                    if (triggerHidden)
                        Plugin.Logger.LogInfo($"Invisible trigger: {item.itemName}");
                }

                if (item.canBeInspected)
                {
                    if (item.toolTips == null)
                        Plugin.Logger.LogWarning($"Item \"{item.name}\" is missing toolTips");
                    else if (item.toolTips.Length < 3)
                    {
                        bool hasInspectTip = false;
                        foreach (string tooltip in item.toolTips)
                        {
                            if (tooltip.StartsWith("Inspect"))
                            {
                                hasInspectTip = true;
                                break;
                            }
                        }

                        if (!hasInspectTip)
                        {
                            item.toolTips = item.toolTips.AddToArray("Inspect: [Z]");
                            Plugin.Logger.LogInfo($"Inspect tooltip: {item.itemName}");
                        }
                    }
                }

                if (conductiveItems.ContainsKey(item.name))
                {
                    item.isConductiveMetal = conductiveItems[item.name] && Plugin.configMakeConductive.Value;
                    Plugin.Logger.LogInfo($"Conductive: {item.itemName} ({item.isConductiveMetal})");
                }
            }
        }

        internal static void OverrideUnlockables()
        {
            foreach (UnlockableItem unlockableItem in StartOfRound.Instance.unlockablesList.unlockables)
            {
                switch (unlockableItem.unlockableName)
                {
                    /*case "Television":
                        unlockableItem.prefabObject.GetComponentInChildren<TVScript>().tvSFX.dopplerLevel = MUSIC_DOPPLER_LEVEL;
                        Plugin.Logger.LogInfo("Doppler level: Television");
                        break;*/
                    case "Record player":
                        unlockableItem.prefabObject.GetComponentInChildren<AnimatedObjectTrigger>().thisAudioSource.dopplerLevel = GlobalReferences.dopplerLevelMult;
                        Plugin.Logger.LogInfo("Doppler level: Record player");
                        break;
                    case "Disco Ball":
                        unlockableItem.prefabObject.GetComponentInChildren<CozyLights>().turnOnAudio.dopplerLevel = 0.92f * GlobalReferences.dopplerLevelMult;
                        Plugin.Logger.LogInfo("Doppler level: Disco ball");
                        break;
                }
            }
        }
    }
}
