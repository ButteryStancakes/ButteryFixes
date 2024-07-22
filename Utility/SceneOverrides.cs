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

            // factory ambience
            Transform bigMachine = GameObject.Find("/Environment/Map/DiageticAmbiance/BigMachine")?.transform;
            bool rotateFireExit = Configuration.fixFireExits.Value;
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
                    break;
                case "Level2Assurance":
                    Plugin.Logger.LogInfo("Detected landing on Assurance");
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
                    break;
                case "Level7Offense":
                    Plugin.Logger.LogInfo("Detected landing on Offense");
                    if (bigMachine != null)
                    {
                        bigMachine.localPosition = new Vector3(27.6018715f, 24.056633f, -67.7034225f);
                        Plugin.Logger.LogInfo("Fixed factory ambience");
                    }
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

            if (!Compatibility.INSTALLED_GENERAL_IMPROVEMENTS)
            {
                GlobalReferences.shipNode = Object.FindObjectsOfType<ScanNodeProperties>().FirstOrDefault(scanNodeProperties => scanNodeProperties.headerText == "Ship")?.transform;
                if (GlobalReferences.shipNode != null)
                    GlobalReferences.shipNodeOffset = GlobalReferences.shipNode.position - GlobalReferences.shipDefaultPos;
            }
        }
    }
}
