﻿using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

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
                        if (!Configuration.unlimitedOldBirds.Value)
                        {
                            enemy.Value.requireNestObjectsToSpawn = true;
                            Plugin.Logger.LogInfo($"{enemy.Value.enemyName}: Require \"nest\" to spawn");
                        }
                        else
                            enemy.Value.requireNestObjectsToSpawn = false;
                        break;
                    case "MaskedPlayerEnemy":
                        enemy.Value.isOutsideEnemy = false;
                        Plugin.Logger.LogInfo($"{enemy.Value.enemyName}: Subtract from indoor power, not outdoor power");
                        break;
                    case "Blob":
                        enemy.Value.canDie = false;
                        Plugin.Logger.LogInfo($"{enemy.Value.enemyName}: Don't \"die\" when crushed by spike trap");
                        break;
                    case "ForestGiant":
                        ScanNodeProperties scanNodeProperties = enemy.Value.enemyPrefab.GetComponentInChildren<ScanNodeProperties>();
                        if (scanNodeProperties != null)
                        {
                            scanNodeProperties.headerText = "Forest Keeper";
                            Plugin.Logger.LogInfo($"{enemy.Value.enemyName}: Rename scan node");
                        }
                        break;
                    case "ClaySurgeon":
                        enemy.Value.enemyPrefab.GetComponent<NavMeshAgent>().speed = 0f;
                        Plugin.Logger.LogInfo($"{enemy.Value.enemyName}: Don't slide around on fresh spawn");
                        foreach (Renderer rend in enemy.Value.enemyPrefab.GetComponentsInChildren<Renderer>())
                        {
                            if (rend.gameObject.layer == 19)
                                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        }
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
                    case "OffenseLevel":
                        selectableLevel.videoReel = null;
                        break;
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
                { "LockPicker", true },
                { "MoldPan", true },
                //{ "Phone", true },
                { "Shotgun", true },
                { "SprayPaint", true },
                //{ "SteeringWheel", true }
            };
            Dictionary<string, bool> grabbableBeforeStart = new()
            {
                { "Airhorn", false },
                { "CashRegister", false },
                { "ChemicalJug", true },
                { "ClownHorn", false },
                { "ComedyMask", false },
                { "TragedyMask", false }
            };
            ScanNodeProperties scanNodeProperties;

            Item shovel = null, tatteredMetalSheet = null;
            foreach (Item item in StartOfRound.Instance.allItemsList.itemsList)
            {
                if (item == null)
                {
                    Plugin.Logger.LogWarning("Encountered a missing item in StartOfRound.allItemsList; this is probably an issue with another mod");
                    continue;
                }

                bool linearRolloff = false;

                switch (item.name)
                {
                    case "Boombox":
                        item.spawnPrefab.GetComponent<BoomboxItem>().boomboxAudio.dopplerLevel = 0.3f * GlobalReferences.dopplerLevelMult;
                        Plugin.Logger.LogInfo("Doppler level: Boombox");
                        break;
                    case "ClownHorn":
                        item.spawnPrefab.GetComponent<NoisemakerProp>().useCooldown = 0.4f;
                        Plugin.Logger.LogInfo("Cooldown: Clown horn");
                        break;
                    case "Cog1":
                    case "EasterEgg":
                    case "FishTestProp":
                    case "MapDevice":
                    case "ZapGun":
                        linearRolloff = true;
                        break;
                    case "ExtensionLadder":
                    case "RadarBooster":
                        item.canBeInspected = false;
                        Plugin.Logger.LogInfo($"Inspectable: {item.itemName} (False)");
                        break;
                    case "Flashlight":
                    case "ProFlashlight":
                        FlashlightItem flashlightItem = item.spawnPrefab.GetComponent<FlashlightItem>();
                        Material[] sharedMaterials = flashlightItem.flashlightMesh.sharedMaterials;
                        sharedMaterials[1] = flashlightItem.bulbDark;
                        flashlightItem.flashlightMesh.sharedMaterials = sharedMaterials;
                        Plugin.Logger.LogInfo($"Bulb off: {item.itemName}");
                        break;
                    case "Hairdryer":
                        item.spawnPrefab.GetComponent<NoisemakerProp>().useCooldown = 2f;
                        Plugin.Logger.LogInfo("Cooldown: Hairdryer");
                        break;
                    case "Key":
                        if (Configuration.keysAreScrap.Value)
                        {
                            item.isScrap = true;
                            Plugin.Logger.LogInfo("Scrap: Key");
                        }
                        else
                        {
                            item.spawnPrefab.GetComponent<KeyItem>().scrapValue = 0;
                            scanNodeProperties = item.spawnPrefab.GetComponentInChildren<ScanNodeProperties>();
                            if (scanNodeProperties != null)
                            {
                                scanNodeProperties.subText = string.Empty;
                                scanNodeProperties.scrapValue = 0;
                                Plugin.Logger.LogInfo("Scan node: Key");
                            }
                        }
                        break;
                    case "Knife":
                        KnifeItem knifeItem = item.spawnPrefab.GetComponent<KnifeItem>();
                        //knifeItem.SetScrapValue(knifeItem.scrapValue);
                        scanNodeProperties = knifeItem.GetComponentInChildren<ScanNodeProperties>();
                        if (scanNodeProperties != null)
                        {
                            scanNodeProperties.scrapValue = knifeItem.scrapValue;
                            scanNodeProperties.subText = $"Value: ${scanNodeProperties.scrapValue}";
                            Plugin.Logger.LogInfo("Scan node: Kitchen knife");
                        }
                        break;
                    case "MagnifyingGlass":
                    case "PillBottle":
                        item.canBeInspected = true;
                        Plugin.Logger.LogInfo($"Inspectable: {item.itemName} (True)");
                        break;
                    case "MetalSheet":
                        tatteredMetalSheet = item;
                        break;
                    case "RedLocustHive":
                        linearRolloff = true;
                        item.spawnPrefab.GetComponent<PhysicsProp>().isInFactory = false;
                        Plugin.Logger.LogInfo("Factory: Hive");
                        break;
                    case "Shovel":
                        shovel = item;
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
                    case "WeedKillerBottle":
                        item.canBeInspected = true;
                        Plugin.Logger.LogInfo("Inspectable: Weed killer (True)");
                        item.spawnPrefab.GetComponent<AudioSource>().rolloffMode = AudioRolloffMode.Logarithmic;
                        Plugin.Logger.LogInfo("Audio rolloff: Weed killer");
                        break;
                }

                if (linearRolloff)
                {
                    item.spawnPrefab.GetComponent<AudioSource>().rolloffMode = AudioRolloffMode.Linear;
                    Plugin.Logger.LogInfo($"Audio rolloff: {item.itemName}");
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
                    item.isConductiveMetal = conductiveItems[item.name] && Configuration.makeConductive.Value;
                    Plugin.Logger.LogInfo($"Conductive: {item.itemName} ({item.isConductiveMetal})");
                }

                if (grabbableBeforeStart.ContainsKey(item.name))
                {
                    item.canBeGrabbedBeforeGameStart = grabbableBeforeStart[item.name];
                    Plugin.Logger.LogInfo($"Hold before ship has landed: {item.itemName} ({item.canBeGrabbedBeforeGameStart})");
                }
            }

            if (tatteredMetalSheet != null && shovel != null)
            {
                tatteredMetalSheet.grabSFX = shovel.grabSFX;
                Plugin.Logger.LogInfo("Audio: Metal sheet");
            }
        }

        internal static void OverrideUnlockables()
        {
            foreach (UnlockableItem unlockableItem in StartOfRound.Instance.unlockablesList.unlockables)
            {
                switch (unlockableItem.unlockableName)
                {
                    /*case "Television":
                        unlockableItem.prefabObject.GetComponentInChildren<TVScript>().tvSFX.dopplerLevel = 0f * MUSIC_DOPPLER_LEVEL;
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
                    case "JackOLantern":
                        unlockableItem.prefabObject.GetComponentInChildren<InteractTrigger>().cooldownTime = 0.5f;
                        Plugin.Logger.LogInfo("Cooldown: Jack o' Lantern");
                        break;
                    case "Plushie pajama man":
                        unlockableItem.prefabObject.GetComponentInChildren<InteractTrigger>().cooldownTime = 0.54f;
                        Plugin.Logger.LogInfo("Cooldown: Plushie pajama man");
                        break;
                    case "Inverse Teleporter":
                        if (!Compatibility.INSTALLED_GENERAL_IMPROVEMENTS)
                        {
                            InteractTrigger buttonTrigger = unlockableItem.prefabObject.GetComponentInChildren<ShipTeleporter>().buttonTrigger;
                            buttonTrigger.hoverTip = buttonTrigger.hoverTip.Replace("Beam up", "Beam out");
                            Plugin.Logger.LogInfo("Text: Inverse teleporter");
                        }
                        break;
                }
            }
        }
    }
}
