using HarmonyLib;
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
                            if (Compatibility.INSTALLED_LETHAL_QUANTITIES)
                            {
                                Plugin.Logger.LogWarning("Config setting \"UnlimitedOldBirds\" is disabled, but you have Lethal Quantities installed. This usually prevents Old Birds from being able to spawn due to a conflict. The config setting will be ignored for this session, but consider enabling it to hide this warning in the future.");
                                enemy.Value.requireNestObjectsToSpawn = false;
                            }
                            else
                            {
                                enemy.Value.requireNestObjectsToSpawn = true;
                                Plugin.Logger.LogDebug($"{enemy.Value.enemyName}: Require \"nest\" to spawn");
                            }
                        }
                        else
                            enemy.Value.requireNestObjectsToSpawn = false;
                        break;
                    case "MaskedPlayerEnemy":
                        enemy.Value.isOutsideEnemy = false;
                        Plugin.Logger.LogDebug($"{enemy.Value.enemyName}: Subtract from indoor power, not outdoor power");
                        break;
                    case "Blob":
                        enemy.Value.canDie = false;
                        Plugin.Logger.LogDebug($"{enemy.Value.enemyName}: Don't \"die\" when crushed by spike trap");
                        break;
                    case "ForestGiant":
                        ScanNodeProperties scanNodeProperties = enemy.Value.enemyPrefab.GetComponentInChildren<ScanNodeProperties>();
                        if (scanNodeProperties != null)
                        {
                            scanNodeProperties.headerText = "Forest Keeper";
                            Plugin.Logger.LogDebug($"{enemy.Value.enemyName}: Rename scan node");
                        }
                        break;
                    case "ClaySurgeon":
                        enemy.Value.enemyPrefab.GetComponent<NavMeshAgent>().speed = 0f;
                        Plugin.Logger.LogDebug($"{enemy.Value.enemyName}: Don't slide around on fresh spawn");
                        foreach (Renderer rend in enemy.Value.enemyPrefab.GetComponentsInChildren<Renderer>())
                        {
                            if (rend.gameObject.layer == 19)
                                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        }
                        break;
                    case "CaveDweller":
                        enemy.Value.enemyPrefab.GetComponent<CaveDwellerPhysicsProp>().itemProperties.isConductiveMetal = false;
                        Plugin.Logger.LogDebug($"Conductive: {enemy.Value.enemyName} (False)");
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
                            Plugin.Logger.LogDebug("Rend now properly spaces spike traps");
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
                { "Clock", false },
                //{ "DustPan", true },
                { "FancyCup", true },
                { "Flask", false },
                //{ "Hairdryer", true },
                { "LockPicker", true },
                { "MoldPan", true },
                //{ "Phone", true },
                { "PlasticCup", false },
                { "Shotgun", true },
                { "SoccerBall", false },
                { "SprayPaint", true },
                //{ "SteeringWheel", true },
                { "ToiletPaperRolls", false },
                { "ToyTrain", false },
                { "Zeddog", false }
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
                        Plugin.Logger.LogDebug("Doppler level: Boombox");
                        break;
                    case "ClownHorn":
                        item.spawnPrefab.GetComponent<NoisemakerProp>().useCooldown = 0.4f;
                        Plugin.Logger.LogDebug("Cooldown: Clown horn");
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
                        Plugin.Logger.LogDebug($"Inspectable: {item.itemName} (False)");
                        break;
                    case "Flashlight":
                    case "ProFlashlight":
                        FlashlightItem flashlightItem = item.spawnPrefab.GetComponent<FlashlightItem>();
                        Material[] sharedMaterials = flashlightItem.flashlightMesh.sharedMaterials;
                        sharedMaterials[1] = flashlightItem.bulbDark;
                        flashlightItem.flashlightMesh.sharedMaterials = sharedMaterials;
                        Plugin.Logger.LogDebug($"Bulb off: {item.itemName}");
                        break;
                    case "Hairdryer":
                        item.spawnPrefab.GetComponent<NoisemakerProp>().useCooldown = 2f;
                        Plugin.Logger.LogDebug("Cooldown: Hairdryer");
                        break;
                    case "Key":
                        if (Configuration.keysAreScrap.Value)
                        {
                            item.isScrap = true;
                            Plugin.Logger.LogDebug("Scrap: Key");
                        }
                        else
                        {
                            item.spawnPrefab.GetComponent<KeyItem>().scrapValue = 0;
                            scanNodeProperties = item.spawnPrefab.GetComponentInChildren<ScanNodeProperties>();
                            if (scanNodeProperties != null)
                            {
                                scanNodeProperties.subText = string.Empty;
                                scanNodeProperties.scrapValue = 0;
                                Plugin.Logger.LogDebug("Scan node: Key");
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
                            Plugin.Logger.LogDebug("Scan node: Kitchen knife");
                        }
                        if (item.minValue == 70 && item.maxValue == 210)
                        {
                            // knife value has never been randomized, but 35 is not the average of 28-84
                            item.maxValue = 105; // 42 + 1
                        }
                        break;
                    case "MagnifyingGlass":
                    case "PillBottle":
                    case "SprayPaint":
                        item.canBeInspected = true;
                        Plugin.Logger.LogDebug($"Inspectable: {item.itemName} (True)");
                        break;
                    case "MetalSheet":
                        tatteredMetalSheet = item;
                        break;
                    case "RedLocustHive":
                        linearRolloff = true;
                        item.spawnPrefab.GetComponent<PhysicsProp>().isInFactory = false;
                        Plugin.Logger.LogDebug("Factory: Hive");
                        if (item.minValue == 90 && item.maxValue == 140)
                        {
                            item.minValue = 100; // 40
                            item.maxValue = 375; // 149 + 1
                        }
                        break;
                    case "Shotgun":
                        if (item.minValue == 30 && item.maxValue == 100)
                        {
                            item.minValue = 63; // 25
                            item.maxValue = 225; // 89 + 1
                        }
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
                                    Plugin.Logger.LogDebug("Meshes: Tragedy");
                                }
                            }
                        }
                        break;
                    case "WeedKillerBottle":
                        item.canBeInspected = true;
                        Plugin.Logger.LogDebug("Inspectable: Weed killer (True)");
                        item.spawnPrefab.GetComponent<AudioSource>().rolloffMode = AudioRolloffMode.Logarithmic;
                        Plugin.Logger.LogDebug("Audio rolloff: Weed killer");
                        break;
                    case "Zeddog":
                        item.dropSFX = item.grabSFX;
                        Plugin.Logger.LogDebug($"Audio: {item.itemName}");
                        break;
                }

                if (linearRolloff)
                {
                    item.spawnPrefab.GetComponent<AudioSource>().rolloffMode = AudioRolloffMode.Linear;
                    Plugin.Logger.LogDebug($"Audio rolloff: {item.itemName}");
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
                        Plugin.Logger.LogDebug($"Invisible trigger: {item.itemName}");
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
                            Plugin.Logger.LogDebug($"Inspect tooltip: {item.itemName}");
                        }
                    }
                }

                if (conductiveItems.ContainsKey(item.name))
                {
                    item.isConductiveMetal = conductiveItems[item.name] && Configuration.makeConductive.Value;
                    Plugin.Logger.LogDebug($"Conductive: {item.itemName} ({item.isConductiveMetal})");
                }

                if (grabbableBeforeStart.ContainsKey(item.name))
                {
                    item.canBeGrabbedBeforeGameStart = grabbableBeforeStart[item.name];
                    Plugin.Logger.LogDebug($"Hold before ship has landed: {item.itemName} ({item.canBeGrabbedBeforeGameStart})");
                }
            }

            if (tatteredMetalSheet != null && shovel != null)
            {
                tatteredMetalSheet.grabSFX = shovel.grabSFX;
                Plugin.Logger.LogDebug("Audio: Metal sheet");
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
                        Plugin.Logger.LogDebug("Doppler level: Television");
                        break;*/
                    case "Record player":
                        unlockableItem.prefabObject.GetComponentInChildren<AnimatedObjectTrigger>().thisAudioSource.dopplerLevel = GlobalReferences.dopplerLevelMult;
                        Plugin.Logger.LogDebug("Doppler level: Record player");
                        break;
                    case "Disco Ball":
                        unlockableItem.prefabObject.GetComponentInChildren<CozyLights>().turnOnAudio.dopplerLevel = 0.92f * GlobalReferences.dopplerLevelMult;
                        Plugin.Logger.LogDebug("Doppler level: Disco ball");
                        break;
                    case "JackOLantern":
                        unlockableItem.prefabObject.GetComponentInChildren<InteractTrigger>().cooldownTime = 0.5f;
                        Plugin.Logger.LogDebug("Cooldown: Jack o' Lantern");
                        break;
                    case "Plushie pajama man":
                        unlockableItem.prefabObject.GetComponentInChildren<InteractTrigger>().cooldownTime = 0.54f;
                        Plugin.Logger.LogDebug("Cooldown: Plushie pajama man");
                        break;
                    case "Inverse Teleporter":
                        if (!Compatibility.INSTALLED_GENERAL_IMPROVEMENTS)
                        {
                            InteractTrigger buttonTrigger = unlockableItem.prefabObject.GetComponentInChildren<ShipTeleporter>().buttonTrigger;
                            buttonTrigger.hoverTip = buttonTrigger.hoverTip.Replace("Beam up", "Beam out");
                            Plugin.Logger.LogDebug("Text: Inverse teleporter");
                        }
                        break;
                }
            }
        }
    }
}
