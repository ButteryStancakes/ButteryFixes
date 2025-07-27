using System.Collections.Generic;
using System.Linq;
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
                    Plugin.Logger.LogDebug("Detected landing on Experimentation");
                    if (bigMachine != null)
                    {
                        bigMachine.localPosition = new(-96.45731f, bigMachine.localPosition.y, bigMachine.localPosition.z);
                        Plugin.Logger.LogDebug("Experimentation - Fixed factory ambience");
                    }
                    // fix fog triggers from the water tower
                    Transform cube7 = GameObject.Find("/Environment/ReverbTriggers (1)/Cube (7)")?.transform;
                    if (cube7 != null)
                    {
                        cube7.localPosition = new(-147.8f, cube7.localPosition.y, -81.2f);
                        cube7.localScale = new(129.6264f, cube7.localScale.y, 184.7249f);
                        Transform cube9 = GameObject.Find("/Environment/ReverbTriggers (1)/Cube (9)")?.transform;
                        if (cube9 != null)
                        {
                            cube9.localPosition = new(-145.4f, cube9.localPosition.y, -42.1f);
                            cube9.localScale = new(171.2598f, cube9.localScale.y, 326.2066f);
                            Transform cube8 = GameObject.Find("/Environment/ReverbTriggers (1)/Cube (8)")?.transform;
                            if (cube8 != null)
                            {
                                cube8.localPosition = new(-117.39f, cube8.localPosition.y, -87.23f);
                                cube8.localScale = new(10.4316f, cube8.localScale.y, 15.95104f);
                                Plugin.Logger.LogDebug("Experimentation - Adjusted water tower fog triggers");
                            }
                        }
                    }
                    Transform steelDoor = GameObject.Find("/Environment/SteelDoor")?.transform;
                    if (steelDoor != null)
                    {
                        steelDoor.localPosition = new(-194.668f, 19.788f, steelDoor.localPosition.z);
                        InteractTrigger cube = steelDoor.Find("DoorMesh/Cube")?.GetComponent<InteractTrigger>();
                        if (cube != null)
                            cube.hoverTip = cube.hoverTip.Replace("[ LMB ]", "[LMB]");
                        Plugin.Logger.LogDebug("Experimentation - Fixed old back entrance");
                    }
                    // hide weird untextured geometry
                    Renderer cube1 = GameObject.Find("/Environment/Map/Cube (1)")?.GetComponent<Renderer>();
                    if (cube1 != null)
                    {
                        cube1.enabled = false;
                        Plugin.Logger.LogDebug("Experimentation - Hide untextured geometry");
                    }
                    GameObject buttresses = GameObject.Find("/Environment/Map/ModelsIntroScene/Cube.007/Buttresses.001");
                    if (buttresses != null)
                    {
                        buttresses.AddComponent<MeshCollider>();
                        Plugin.Logger.LogDebug("Experimentation - Fix buttress collision");
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
                    GameObject cube002 = GameObject.Find("/Environment/Map/CementFacility1/Cube.002");
                    if (cube002 != null)
                    {
                        GameObject cube002_2 = new("Cube.002 (2)");
                        cube002_2.transform.SetParent(cube002.transform.parent);
                        cube002_2.transform.SetLocalPositionAndRotation(new(-24.297f, cube002.transform.localPosition.y, 19.699f), cube002.transform.localRotation);
                        cube002_2.transform.localScale = new(cube002.transform.localScale.x, 0.655802f, cube002.transform.localScale.z);
                        cube002_2.layer = cube002.layer;
                        cube002_2.AddComponent<MeshFilter>().sharedMesh = cube002.GetComponent<MeshCollider>().sharedMesh;
                        Renderer cubeRend = cube002.GetComponent<Renderer>();
                        cube002_2.AddComponent<MeshRenderer>().sharedMaterial = cubeRend.sharedMaterial;
                        cubeRend.enabled = false;
                        Plugin.Logger.LogDebug("Rend - Adjust fire exit visuals");
                    }
                    if (bigMachine != null)
                    {
                        bigMachine.localPosition = new(63.5566711f, -18.0999107f, -139.493729f);
                        Plugin.Logger.LogDebug("Rend - Fixed factory ambience");
                    }
                    break;
                case "Level6Dine":
                    Plugin.Logger.LogDebug("Detected landing on Dine");
                    // fix death pit in front of fire exit
                    Transform killTrigger4 = GameObject.Find("/Environment/Map/KillTrigger (4)")?.transform;
                    if (killTrigger4 != null)
                    {
                        killTrigger4.localPosition = new(148.11f, killTrigger4.localPosition.y, 83.61f);
                        killTrigger4.localScale = new(35.3778f, killTrigger4.localScale.y, killTrigger4.localScale.z);
                        Plugin.Logger.LogDebug("Dine - Fixed death pit");
                    }
                    break;
                case "Level7Offense":
                    Plugin.Logger.LogDebug("Detected landing on Offense");
                    if (bigMachine != null)
                    {
                        bigMachine.localPosition = new(27.6018715f, 24.056633f, -67.7034225f);
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
                        bigMachine.localPosition = new(-36.0699997f, 55.1199989f, 26.1499996f);
                        Plugin.Logger.LogDebug("Titan - Fixed factory ambience");
                    }
                    break;
                case "Level9Artifice":
                    Plugin.Logger.LogDebug("Detected landing on Artifice");
                    GameObject circularRoofBeam = GameObject.Find("/Environment/Map/WarehouseV1 (2)/CircularRoofBeam");
                    if (circularRoofBeam != null)
                    {
                        circularRoofBeam.SetActive(false);
                        Plugin.Logger.LogDebug("Artifice - Hide out-of-bounds objects");
                    }
                    Transform fireExitDoor = GameObject.Find("/Environment/FireExit/FireExitDoorContainer/FireExitDoor")?.transform;
                    if (fireExitDoor != null)
                    {
                        fireExitDoor.localPosition = new(12.356f, fireExitDoor.localPosition.y, -13.3f);
                        Plugin.Logger.LogDebug("Artifice - Adjust fire exit position");
                    }
                    /*if (bigMachine != null)
                    {
                        bigMachine.localPosition = new(bigMachine.localPosition.x, 14.2f, bigMachine.localPosition.z);
                        Plugin.Logger.LogDebug("Artifice - Fixed factory ambience");
                    }*/
                    if (Configuration.restoreArtificeAmbience.Value)
                    {
                        AudioSource nightTimeSilenceBass = GameObject.Find("/Systems/Audio/HighAndLowAltitudeBG/LowAudio")?.GetComponent<AudioSource>();
                        if (nightTimeSilenceBass != null)
                        {
                            nightTimeSilenceBass.playOnAwake = true;
                            if (!nightTimeSilenceBass.isPlaying)
                                nightTimeSilenceBass.Play();
                            Plugin.Logger.LogDebug("Artifice - Restored nighttime ambience");
                        }
                    }
                    break;
                case "Level10Adamance":
                    Plugin.Logger.LogDebug("Detected landing on Adamance");
                    if (bigMachine != null)
                    {
                        bigMachine.localPosition = new(-108.444908f, -3.29539537f, 8.0433712f);
                        Plugin.Logger.LogDebug("Adamance - Fixed factory ambience");
                    }
                    rotateFireExit = false;
                    break;
                case "Level11Embrion":
                    Plugin.Logger.LogDebug("Detected landing on Embrion");
                    if (bigMachine != null)
                    {
                        bigMachine.localPosition = new(202.604599f, 14.0158f, 3.28045521f);
                        Plugin.Logger.LogDebug("Embrion - Fixed factory ambience");
                    }
                    break;
                case "CompanyBuilding":
                    Plugin.Logger.LogDebug("Detected landing on Gordion");
                    GameObject scanNodeMainEntrance = GameObject.Find("/Environment/ScanNodes/ScanNode");
                    if (scanNodeMainEntrance != null)
                    {
                        scanNodeMainEntrance.SetActive(false);
                        Plugin.Logger.LogDebug("Gordion - Disabled \"Main entrance\" scan node");
                    }
                    break;
                default:
                    Plugin.Logger.LogInfo("Landed on unknown moon");
                    rotateFireExit = false;
                    break;
            }

            // fire exits
            if (rotateFireExit)
            {
                foreach (EntranceTeleport entranceTeleport in Object.FindObjectsByType<EntranceTeleport>(FindObjectsSortMode.None))
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
                GlobalReferences.shipNode = Object.FindObjectsByType<ScanNodeProperties>(FindObjectsSortMode.None).FirstOrDefault(scanNodeProperties => scanNodeProperties.headerText == "Ship")?.transform;
                if (GlobalReferences.shipNode != null)
                    GlobalReferences.shipNodeOffset = GlobalReferences.shipNode.position - GlobalReferences.shipDefaultPos;
            }

            // fix contour map not always appearing
            if (StartOfRound.Instance != null)
                StartOfRound.Instance.mapScreen.checkedForContourMap = false;
        }
    }
}
