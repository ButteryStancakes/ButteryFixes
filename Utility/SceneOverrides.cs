﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ButteryFixes.Utility
{
    internal static class SceneOverrides
    {
        internal static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Additive)
                return;

            bool rotateFireExit = Configuration.fixFireExits.Value;
            // factory ambience
            Transform bigMachine = GameObject.Find("/Environment/Map/DiageticAmbiance/BigMachine")?.transform;
            // for unused objects underneath the map
            List<string> modelsIntroScene = [];
            switch (scene.name)
            {
                case "Level1Experimentation":
                    Plugin.Logger.LogInfo("Detected landing on Experimentation");
                    if (bigMachine != null)
                    {
                        bigMachine.localPosition = new Vector3(-112.04f, bigMachine.localPosition.y, bigMachine.localPosition.z);
                        Plugin.Logger.LogInfo("Fixed factory ambience");
                    }
                    // fix fog triggers from the water tower
                    Transform cube7 = GameObject.Find("/Environment/ReverbTriggers (1)/Cube (7)")?.transform;
                    if (cube7 != null)
                    {
                        cube7.localPosition = new Vector3(-147.8f, cube7.localPosition.y, -81.2f);
                        cube7.localScale = new Vector3(129.6264f, cube7.localScale.y, 184.7249f);
                        Transform cube9 = GameObject.Find("/Environment/ReverbTriggers (1)/Cube (9)")?.transform;
                        if (cube9 != null)
                        {
                            cube9.localPosition = new Vector3(-145.4f, cube9.localPosition.y, -42.1f);
                            cube9.localScale = new Vector3(171.2598f, cube9.localScale.y, 326.2066f);
                            Transform cube8 = GameObject.Find("/Environment/ReverbTriggers (1)/Cube (8)")?.transform;
                            if (cube8 != null)
                            {
                                cube8.localPosition = new Vector3(-117.39f, cube8.localPosition.y, -87.23f);
                                cube8.localScale = new Vector3(10.4316f, cube8.localScale.y, 15.95104f);
                                Plugin.Logger.LogInfo("Adjusted water tower fog triggers");
                            }
                        }
                    }
                    Transform steelDoor = GameObject.Find("/Environment/SteelDoor")?.transform;
                    if (steelDoor != null)
                    {
                        steelDoor.localPosition = new Vector3(-194.668f, 19.788f, steelDoor.localPosition.z);
                        Plugin.Logger.LogInfo("Fixed old back entrance");
                    }
                    // hide weird untextured geometry
                    Renderer cube = GameObject.Find("/Environment/Map/Cube (1)")?.GetComponent<Renderer>();
                    if (cube != null)
                    {
                        cube.enabled = false;
                        Plugin.Logger.LogInfo("Hide untextured geometry");
                    }
                    modelsIntroScene.AddRange([
                        "BendingPipe",
                        "Bolt",
                        "CatwalkChunk",
                        "CatwalkChunk.004",
                        "CatwalkChunk.005",
                        "CatwalkRailPost",
                        "CatwalkStairTile",
                        "ChainlinkFenceBend",
                        "ChainlinkFenceCut",
                        "CordConnector",
                        "Cube.001",
                        "Cube.004",
                        "Cube.006",
                        "Girder1",
                        "GridPlate",
                        "LadderFrame",
                        "LongCord1",
                        "LongCord2",
                        "LongCord3",
                        "LongCord4",
                        "MeterBoxDevice",
                        "MiscShelf1",
                        "MiscShelf2",
                        "NurbsPath",
                        "Pipework2",
                        "PlayerScaleCube.001",
                        "PlayerScaleRef",
                        "PlayerScaleRef.001",
                        "RaisedCementPlatform",
                        "Scaffolding",
                        "SiloTall",
                        "SpawnRoom",
                        "StandardDoorSize",
                        "StandardDoorSize.001",
                        "StandardDoorSize.002",
                        "TelephonePoleCordsC",
                        "TrainCarRailLowDetail",
                        "ValveWithHandle.001"
                    ]);
                    break;
                case "Level2Assurance":
                    Plugin.Logger.LogInfo("Detected landing on Assurance");
                    modelsIntroScene.AddRange([
                        "ChainlinkFenceCut",
                        "NurbsPath",
                        "Pipework2",
                        "PlayerScaleCube.001",
                        "PlayerScaleRef",
                        "PlayerScaleRef.001"
                    ]);
                    break;
                case "Level3Vow":
                    Plugin.Logger.LogInfo("Detected landing on Vow");
                    rotateFireExit = false;
                    break;
                case "Level4March":
                    Plugin.Logger.LogInfo("Detected landing on March");
                    break;
                case "Level5Rend":
                    Plugin.Logger.LogInfo("Detected landing on Rend");
                    SetUpFancyEntranceDoors(new Vector3(50.5449982f, -16.8225021f, -152.716583f), Quaternion.Euler(-90f, 180f, 64.342f));
                    break;
                case "Level6Dine":
                    Plugin.Logger.LogInfo("Detected landing on Dine");
                    // fix death pit in front of fire exit
                    Transform killTrigger4 = GameObject.Find("/Environment/Map/KillTrigger (4)")?.transform;
                    if (killTrigger4 != null)
                    {
                        killTrigger4.localPosition = new Vector3(148.11f, killTrigger4.localPosition.y, 83.61f);
                        killTrigger4.localScale = new Vector3(35.3778f, killTrigger4.localScale.y, killTrigger4.localScale.z);
                        Plugin.Logger.LogInfo("Fixed fire exit death pit");
                    }
                    SetUpFancyEntranceDoors(new Vector3(-156.552994f, -15.1260004f, 12.1549997f), Quaternion.Euler(-90f, -90f, 84.136f));
                    break;
                case "Level7Offense":
                    Plugin.Logger.LogInfo("Detected landing on Offense");
                    if (bigMachine != null)
                    {
                        bigMachine.localPosition = new Vector3(27.6018715f, 24.056633f, -67.7034225f);
                        Plugin.Logger.LogInfo("Fixed factory ambience");
                    }
                    modelsIntroScene.AddRange([
                        "ChainlinkFenceCut",
                        "CreeperVine",
                        "MiscShelf1",
                        "NurbsPath",
                        "Pipework2",
                        "PlayerScaleCube.001",
                        "PlayerScaleRef",
                        "PlayerScaleRef.001",
                        "LargePipeCorner (1)",
                        "LargePipeCorner (2)",
                        "Girder1 (4)"
                    ]);
                    break;
                case "Level8Titan":
                    Plugin.Logger.LogInfo("Detected landing on Titan");
                    if (bigMachine != null)
                    {
                        bigMachine.localPosition = new Vector3(-36.0699997f, 55.1199989f, 26.1499996f);
                        Plugin.Logger.LogInfo("Fixed factory ambience");
                    }
                    break;
                case "Level9Artifice":
                    Plugin.Logger.LogInfo("Detected landing on Artifice");
                    SetUpFancyEntranceDoors(new Vector3(52.3199997f, -0.665000021f, -156.145996f), Quaternion.Euler(-90f, -90f, 0f));
                    break;
                case "Level10Adamance":
                    Plugin.Logger.LogInfo("Detected landing on Adamance");
                    if (bigMachine != null)
                    {
                        bigMachine.localPosition = new Vector3(-108.444908f, -3.29539537f, 8.0433712f);
                        Plugin.Logger.LogInfo("Fixed factory ambience");
                    }
                    rotateFireExit = false;
                    break;
                case "Level11Embrion":
                    Plugin.Logger.LogInfo("Detected landing on Embrion");
                    if (bigMachine != null)
                    {
                        bigMachine.localPosition = new Vector3(202.604599f, 14.0158f, 3.28045521f);
                        Plugin.Logger.LogInfo("Fixed factory ambience");
                    }
                    break;
                case "CompanyBuilding":
                    Plugin.Logger.LogInfo("Detected landing on Gordion");
                    break;
                default:
                    Plugin.Logger.LogInfo("Landed on unknown moon");
                    rotateFireExit = false;
                    break;
            }

            // fire exits
            if (rotateFireExit)
            {
                foreach (EntranceTeleport entranceTeleport in Object.FindObjectsOfType<EntranceTeleport>())
                {
                    if (entranceTeleport.isEntranceToBuilding && entranceTeleport.entranceId > 0)
                    {
                        entranceTeleport.entrancePoint.localRotation = Quaternion.Euler(0f, 180f, 0f);
                        Plugin.Logger.LogInfo($"Fixed rotation of external fire exit #{entranceTeleport.entranceId}");
                    }
                }
            }

            // hide out-of-bounds objects
            if (modelsIntroScene.Count > 0)
            {
                Transform modelsIntroSceneParent = GameObject.Find("Environment/Map/ModelsIntroScene")?.transform;
                foreach (Transform modelIntroScene in modelsIntroSceneParent)
                {
                    if (modelsIntroScene.Remove(modelIntroScene.name))
                        modelIntroScene.gameObject.SetActive(false);
                }
                Plugin.Logger.LogInfo("Hide out-of-bounds objects (Experimentation and leftovers)");
                if (modelsIntroScene.Count > 0)
                {
                    Plugin.Logger.LogWarning($"Failed to hide {modelsIntroScene.Count} objects:");
                    foreach (string modelIntroSceneName in modelsIntroScene)
                        Plugin.Logger.LogWarning($"- \"{modelIntroSceneName}\"");
                }
            }

            if (!Compatibility.INSTALLED_GENERAL_IMPROVEMENTS)
            {
                GlobalReferences.shipNode = Object.FindObjectsOfType<ScanNodeProperties>().FirstOrDefault(scanNodeProperties => scanNodeProperties.headerText == "Ship")?.transform;
                if (GlobalReferences.shipNode != null)
                    GlobalReferences.shipNodeOffset = GlobalReferences.shipNode.position - GlobalReferences.shipDefaultPos;
            }
        }

        static void SetUpFancyEntranceDoors(Vector3 pos, Quaternion rot)
        {
            if (!Configuration.fancyEntranceDoors.Value)
                return;

            GameObject steelDoorFake = GameObject.Find("SteelDoorFake");
            GameObject steelDoorFake2 = GameObject.Find("SteelDoorFake (1)");
            if (steelDoorFake != null && steelDoorFake2 != null)
            {
                Transform plane = steelDoorFake.transform.parent.Find("Plane");
                Transform doorFrame = steelDoorFake.transform.parent.Find("DoorFrame (1)");
                if (plane != null && doorFrame != null)
                {
                    GameObject wideDoorFrame = null;
                    try
                    {
                        AssetBundle fancyEntranceDoors = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "fancyentrancedoors"));
                        wideDoorFrame = fancyEntranceDoors.LoadAsset<GameObject>("WideDoorFrame");
                        fancyEntranceDoors.Unload(false);
                    }
                    catch
                    {
                        Plugin.Logger.LogError("Encountered some error loading assets from bundle \"fancyentrancedoors\". Did you install the plugin correctly?");
                    }

                    if (wideDoorFrame != null)
                    {
                        steelDoorFake.SetActive(false);
                        steelDoorFake2.SetActive(false);

                        Object.Instantiate(wideDoorFrame, pos, rot);

                        doorFrame.localScale = new Vector3(doorFrame.localScale.x, doorFrame.localScale.y + 0.05f, doorFrame.localScale.z);

                        plane.localPosition = new Vector3(plane.localPosition.x, plane.localPosition.y - 1f, plane.localPosition.z);
                        plane.localScale = new Vector3(plane.localScale.x + 0.047f, plane.localScale.y, plane.localScale.z + 0.237f);

                        Plugin.Logger.LogInfo("Manor map: Use fancy doors at main entrance");
                    }
                    else
                        Plugin.Logger.LogWarning("The \"FancyEntranceDoors\" setting is enabled, but will be skipped because there was an error loading the manor door assets.");
                }
                else
                    Plugin.Logger.LogWarning("The \"FancyEntranceDoors\" setting is enabled, but will be skipped because the \"darkness plane\" can not be found.");
            }
            else
                Plugin.Logger.LogWarning("The \"FancyEntranceDoors\" setting is enabled, but will be skipped because the factory doors can not be found.");
        }
    }
}
