using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace ButteryFixes.Utility
{
    internal static class ScriptableObjectOverrides
    {
        internal static void OverrideEnemyTypes()
        {
            foreach (KeyValuePair<string, EnemyType> enemy in GlobalReferences.allEnemiesList)
            {
                switch (enemy.Key)
                {
                    case "RadMech":
                        ScanNodeProperties scanOldBird = enemy.Value.nestSpawnPrefab.GetComponentInChildren<ScanNodeProperties>();
                        if (scanOldBird != null)
                        {
                            scanOldBird.requiresLineOfSight = false;
                            scanOldBird.transform.localPosition = new(0f, scanOldBird.transform.localPosition.y, 0f);
                            Plugin.Logger.LogDebug($"{enemy.Value.enemyName}: Scan on both sides");
                        }

                        break;
                    case "Blob":
                        if (!Compatibility.INSTALLED_EVERYTHING_CAN_DIE)
                        {
                            enemy.Value.canDie = false;
                            Plugin.Logger.LogDebug($"{enemy.Value.enemyName}: Don't \"die\" when crushed by spike trap");
                        }
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
                    case "SandWorm":
                        enemy.Value.canBeDestroyed = false;
                        Plugin.Logger.LogDebug($"{enemy.Value.enemyName}: Don't get eaten by other worms");
                        break;
                }
                // fix residue in ScriptableObject
                enemy.Value.numberSpawned = 0;
                enemy.Value.hasSpawnedAtLeastOne = false;
            }
        }

        internal static void OverrideSelectableLevels()
        {
            foreach (SelectableLevel selectableLevel in StartOfRound.Instance.levels)
            {
                switch (selectableLevel.name)
                {
                    case "OffenseLevel":
                        if (selectableLevel.videoReel != null && selectableLevel.videoReel.name == "MapView220Ass")
                        {
                            selectableLevel.videoReel = null;
                            Plugin.Logger.LogDebug("Offense: Video reel");
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
                //{ "ControlPad", false },
                { "DustPan", true },
                { "FancyCup", !Compatibility.INSTALLED_UPTURNED_VARIETY },
                { "LockPicker", true },
                //{ "MagnifyingGlass", true },
                //{ "Phone", true },
                { "Shotgun", true },
                { "SprayPaint", true },
                //{ "SteeringWheel", true },
            };
            Dictionary<string, bool> grabbableBeforeStart = new()
            {
                { "Airhorn", false },
                { "CashRegister", false },
                { "ChemicalJug", true },
                { "ClownHorn", false },
                { "ComedyMask", false },
                { "RubberDuck", false },
                { "TragedyMask", false }
            };
            Dictionary<string, bool> inspectable = new()
            {
                { "BabyKiwiEgg", false },
                { "PillBottle", true },
                { "SprayPaint", true },
                { "WeedKillerBottle", true }
            };
            ScanNodeProperties scanNodeProperties;

            foreach (Item item in StartOfRound.Instance.allItemsList.itemsList)
            {
                if (item == null)
                {
                    Plugin.Logger.LogWarning("Encountered a missing item in StartOfRound.allItemsList; this is probably an issue with another mod");
                    continue;
                }

                scanNodeProperties = item.spawnPrefab?.GetComponentInChildren<ScanNodeProperties>();

                switch (item.name)
                {
                    case "BabyKiwiEgg":
                        item.spawnPrefab.GetComponent<KiwiBabyItem>().isInFactory = false;
                        Plugin.Logger.LogDebug("Factory: Egg");
                        break;
                    case "CashRegister":
                        if (Configuration.adjustCooldowns.Value)
                        {
                            item.spawnPrefab.GetComponent<NoisemakerProp>().useCooldown = 2.35f;
                            Plugin.Logger.LogDebug("Cooldown: Cash register");
                        }
                        break;
                    case "ClownHorn":
                        if (Configuration.adjustCooldowns.Value)
                        {
                            item.spawnPrefab.GetComponent<NoisemakerProp>().useCooldown = 0.35f;
                            Plugin.Logger.LogDebug("Cooldown: Clown horn");
                        }
                        break;
                    case "FancyCup":
                        if (Configuration.theGoldenGoblet.Value)
                        {
                            if (scanNodeProperties != null)
                            {
                                scanNodeProperties.headerText = scanNodeProperties.headerText.Replace("Golden cup", "Golden goblet");
                                Plugin.Logger.LogDebug("Scan node: Golden cup");
                            }

                            item.itemName = item.itemName.Replace("Golden cup", "Golden goblet");
                            Plugin.Logger.LogDebug("Name: Golden cup");
                        }
                        break;
                    case "FancyLamp":
                        item.verticalOffset = 0f;
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
                        if (Configuration.adjustCooldowns.Value)
                        {
                            item.spawnPrefab.GetComponent<NoisemakerProp>().useCooldown = 1.35f;
                            Plugin.Logger.LogDebug("Cooldown: Hairdryer");
                        }
                        break;
                    case "Knife":
                        KnifeItem knifeItem = item.spawnPrefab.GetComponent<KnifeItem>();
                        //knifeItem.SetScrapValue(knifeItem.scrapValue);
                        if (scanNodeProperties != null)
                        {
                            scanNodeProperties.scrapValue = knifeItem.scrapValue;
                            scanNodeProperties.subText = $"Value: ${scanNodeProperties.scrapValue}";
                            Plugin.Logger.LogDebug("Scan node: Kitchen knife");
                        }
                        break;
                    case "RedLocustHive":
                        item.spawnPrefab.GetComponent<PhysicsProp>().isInFactory = false;
                        Plugin.Logger.LogDebug("Factory: Hive");
                        break;
                    case "SeveredHeart":
                        LoopShapeKey[] loopShapeKeys = item.spawnPrefab.GetComponentsInChildren<LoopShapeKey>();
                        if (loopShapeKeys != null && loopShapeKeys.Length > 0)
                        {
                            foreach (LoopShapeKey loopShapeKey in loopShapeKeys)
                            {
                                if (loopShapeKey.GetComponent<HeartHack>() == null)
                                    loopShapeKey.gameObject.AddComponent<HeartHack>().loopShapeKey = loopShapeKey;
                            }

                            Plugin.Logger.LogDebug("Animation: Heart");
                        }
                        break;
                    case "SeveredThigh":
                        LODGroup lodGroup = item.spawnPrefab.GetComponent<LODGroup>();
                        LOD[] lods = lodGroup.GetLODs();
                        lods[0].screenRelativeTransitionHeight = 0.15f; // 60% is too high
                        lodGroup.SetLODs(lods);
                        Plugin.Logger.LogDebug("LoDs: Knee");
                        break;
                    case "SeveredTongue":
                        item.syncDiscardFunction = true;
                        Plugin.Logger.LogDebug("Sync: Tongue");
                        break;
                }

                if (item.spawnPrefab != null)
                {
                    // affects whoopie cushion primarily
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

                    // affects soccer ball primarily
                    if (item.spawnPrefab.GetComponent<Rigidbody>() != null && scanNodeProperties != null)
                    {
                        Rigidbody rb = scanNodeProperties.GetComponent<Rigidbody>();
                        if (rb == null)
                        {
                            rb = scanNodeProperties.gameObject.AddComponent<Rigidbody>();
                            rb.isKinematic = true;
                            Plugin.Logger.LogDebug($"Scan node rigidbody: {item.itemName}");
                        }
                    }
                }

                if (inspectable.ContainsKey(item.name))
                {
                    item.canBeInspected = inspectable[item.name];
                    Plugin.Logger.LogDebug($"Inspectable: {item.itemName} ({item.canBeInspected})");
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

                if (grabbableBeforeStart.ContainsKey(item.name) && !Compatibility.INSTALLED_GENERAL_IMPROVEMENTS)
                {
                    item.canBeGrabbedBeforeGameStart = grabbableBeforeStart[item.name];
                    Plugin.Logger.LogDebug($"Hold before ship has landed: {item.itemName} ({item.canBeGrabbedBeforeGameStart})");
                }
            }
        }

        internal static void OverrideUnlockables()
        {
            foreach (UnlockableItem unlockableItem in StartOfRound.Instance.unlockablesList.unlockables)
            {
                switch (unlockableItem.unlockableName)
                {
                    case "JackOLantern":
                        if (Configuration.adjustCooldowns.Value)
                        {
                            unlockableItem.prefabObject.GetComponentInChildren<InteractTrigger>().cooldownTime = 0.45f;
                            Plugin.Logger.LogDebug("Cooldown: Jack o' Lantern");
                        }
                        break;
                    case "Plushie pajama man":
                        if (Configuration.adjustCooldowns.Value)
                        {
                            unlockableItem.prefabObject.GetComponentInChildren<InteractTrigger>().cooldownTime = 0.2f;
                            Plugin.Logger.LogDebug("Cooldown: Plushie pajama man");
                        }
                        break;
                    case "Inverse Teleporter":
                        if (!Compatibility.INSTALLED_GENERAL_IMPROVEMENTS)
                        {
                            InteractTrigger buttonTrigger = unlockableItem.prefabObject.GetComponentInChildren<ShipTeleporter>().buttonTrigger;
                            buttonTrigger.hoverTip = buttonTrigger.hoverTip.Replace("Beam up", "Beam out");
                            Plugin.Logger.LogDebug("Text: Inverse teleporter");
                        }
                        break;
                    case "Microwave":
                        Transform microwaveBody = unlockableItem.prefabObject.transform.Find("MicrowaveBody");
                        microwaveBody.gameObject.layer = 8;
                        Plugin.Logger.LogDebug("Collision: Microwave");
                        InteractTrigger microwaveTrigger = microwaveBody.Find("MicrowaveDoor/DoorButtonClose").GetComponent<InteractTrigger>();
                        microwaveTrigger.hoverTip = microwaveTrigger.hoverTip.Replace("Store item", "Use microwave");
                        Plugin.Logger.LogDebug("Text: Microwave");
                        break;
                    case "Fridge":
                        Transform[] fridgeColliders = unlockableItem.prefabObject.GetComponentsInChildren<Transform>();
                        foreach (Transform fridgeCollider in fridgeColliders)
                        {
                            if (fridgeCollider.name == "FridgeBody" || fridgeCollider.gameObject.layer == 6)
                                fridgeCollider.gameObject.layer = 8;
                            else if (fridgeCollider.GetComponent<PlaceableObjectsSurface>() != null)
                                fridgeCollider.GetComponent<Collider>().isTrigger = false; // so the raycasts connect
                        }
                        Plugin.Logger.LogDebug("Collision: Fridge");
                        break;
                }

                /*if (unlockableItem.unlockableType == 0 && unlockableItem.headCostumeObject != null)
                {
                    if (unlockableItem.headCostumeObject.name.StartsWith("PartyHatContainer"))
                    {
                        Transform birthdayHat = unlockableItem.headCostumeObject.transform.Find("BirthdayHat");
                        if (birthdayHat != null)
                        {
                            birthdayHat.SetLocalPositionAndRotation(new Vector3(-0.0309999995f, 0.286000013f, -0.00700000022f), Quaternion.Euler(255.292f, 0f, 180f));
                            Plugin.Logger.LogDebug($"Offset: Birthday hat ({unlockableItem.unlockableName})");
                        }
                    }
                }*/
            }
        }
    }
}
