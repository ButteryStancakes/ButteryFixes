using System.Collections.Generic;
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
            string cabinDoor = string.Empty;
            switch (scene.name)
            {
                case "Level1Experimentation":
                    Plugin.Logger.LogDebug("Detected landing on Experimentation");
                    if (bigMachine != null)
                    {
                        bigMachine.localPosition = new Vector3(-112.04f, bigMachine.localPosition.y, bigMachine.localPosition.z);
                        Plugin.Logger.LogDebug("Experimentation - Fixed factory ambience");
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
                                Plugin.Logger.LogDebug("Experimentation - Adjusted water tower fog triggers");
                            }
                        }
                    }
                    Transform steelDoor = GameObject.Find("/Environment/SteelDoor")?.transform;
                    if (steelDoor != null)
                    {
                        steelDoor.localPosition = new Vector3(-194.668f, 19.788f, steelDoor.localPosition.z);
                        Plugin.Logger.LogDebug("Experimentation - Fixed old back entrance");
                    }
                    // hide weird untextured geometry
                    Renderer cube = GameObject.Find("/Environment/Map/Cube (1)")?.GetComponent<Renderer>();
                    if (cube != null)
                    {
                        cube.enabled = false;
                        Plugin.Logger.LogDebug("Experimentation - Hide untextured geometry");
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
                        "Cube.007/HangarRoomBeams.001",
                        "Cube.007/SiloWithLadder.001",
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
                    Plugin.Logger.LogDebug("Detected landing on Assurance");
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
                    Plugin.Logger.LogDebug("Detected landing on Vow");
                    rotateFireExit = false;
                    break;
                case "Level4March":
                    Plugin.Logger.LogDebug("Detected landing on March");
                    break;
                case "Level5Rend":
                    Plugin.Logger.LogDebug("Detected landing on Rend");
                    cabinDoor = "/Environment/Map/SnowCabin/FancyDoorMapModel/SteelDoor (1)/DoorMesh/Cube";
                    break;
                case "Level6Dine":
                    Plugin.Logger.LogDebug("Detected landing on Dine");
                    // fix death pit in front of fire exit
                    Transform killTrigger4 = GameObject.Find("/Environment/Map/KillTrigger (4)")?.transform;
                    if (killTrigger4 != null)
                    {
                        killTrigger4.localPosition = new Vector3(148.11f, killTrigger4.localPosition.y, 83.61f);
                        killTrigger4.localScale = new Vector3(35.3778f, killTrigger4.localScale.y, killTrigger4.localScale.z);
                        Plugin.Logger.LogDebug("Dine - Fixed death pit");
                    }
                    break;
                case "Level7Offense":
                    Plugin.Logger.LogDebug("Detected landing on Offense");
                    if (bigMachine != null)
                    {
                        bigMachine.localPosition = new Vector3(27.6018715f, 24.056633f, -67.7034225f);
                        Plugin.Logger.LogDebug("Offense - Fixed factory ambience");
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
                    Plugin.Logger.LogDebug("Detected landing on Titan");
                    if (bigMachine != null)
                    {
                        bigMachine.localPosition = new Vector3(-36.0699997f, 55.1199989f, 26.1499996f);
                        Plugin.Logger.LogDebug("Titan - Fixed factory ambience");
                    }
                    break;
                case "Level9Artifice":
                    Plugin.Logger.LogDebug("Detected landing on Artifice");
                    break;
                case "Level10Adamance":
                    Plugin.Logger.LogDebug("Detected landing on Adamance");
                    if (bigMachine != null)
                    {
                        bigMachine.localPosition = new Vector3(-108.444908f, -3.29539537f, 8.0433712f);
                        Plugin.Logger.LogDebug("Adamance - Fixed factory ambience");
                    }
                    rotateFireExit = false;
                    cabinDoor = "/Environment/SnowCabin/FancyDoorMapModel/SteelDoor (1)/DoorMesh/Cube";
                    break;
                case "Level11Embrion":
                    Plugin.Logger.LogDebug("Detected landing on Embrion");
                    if (bigMachine != null)
                    {
                        bigMachine.localPosition = new Vector3(202.604599f, 14.0158f, 3.28045521f);
                        Plugin.Logger.LogDebug("Embrion - Fixed factory ambience");
                    }
                    break;
                case "CompanyBuilding":
                    Plugin.Logger.LogDebug("Detected landing on Gordion");
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
                        Plugin.Logger.LogDebug($"Fixed rotation of external fire exit #{entranceTeleport.entranceId}");
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
                Plugin.Logger.LogDebug("Hide out-of-bounds objects (Experimentation and leftovers)");
                if (modelsIntroScene.Count > 0)
                {
                    int count = 0;
                    foreach (string modelIntroSceneName in modelsIntroScene)
                    {
                        Transform modelIntroScene2 = modelsIntroSceneParent.Find(modelIntroSceneName);
                        if (modelIntroScene2 != null)
                        {
                            modelIntroScene2.gameObject.SetActive(!modelIntroScene2.gameObject.activeSelf);
                            count++;
                        }
                    }
                    if (modelsIntroScene.Count != count)
                    {
                        Plugin.Logger.LogWarning($"Failed to hide {modelsIntroScene.Count} objects:");
                        foreach (string modelIntroSceneName in modelsIntroScene)
                            Plugin.Logger.LogWarning($"- \"{modelIntroSceneName}\"");
                    }
                }
            }

            // move the ship node with the ship as it's landing or taking off
            if (!Compatibility.INSTALLED_GENERAL_IMPROVEMENTS)
            {
                GlobalReferences.shipNode = Object.FindObjectsOfType<ScanNodeProperties>().FirstOrDefault(scanNodeProperties => scanNodeProperties.headerText == "Ship")?.transform;
                if (GlobalReferences.shipNode != null)
                    GlobalReferences.shipNodeOffset = GlobalReferences.shipNode.position - GlobalReferences.shipDefaultPos;
            }

            // override cabin door with wooden sounds
            if (!string.IsNullOrEmpty(cabinDoor) && GlobalReferences.woodenDoorOpen != null && GlobalReferences.woodenDoorOpen.Length > 0 && GlobalReferences.woodenDoorClose != null && GlobalReferences.woodenDoorClose.Length > 0)
            {
                AnimatedObjectTrigger door = GameObject.Find(cabinDoor)?.GetComponent<AnimatedObjectTrigger>();
                if (door != null)
                {
                    door.boolFalseAudios = GlobalReferences.woodenDoorClose;
                    door.boolTrueAudios = GlobalReferences.woodenDoorOpen;
                    Plugin.Logger.LogDebug("Overwritten cabin door SFX with wooden variants");
                }
            }
        }
    }
}
