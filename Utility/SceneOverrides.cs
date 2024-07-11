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
            bool rotateFireExit = Plugin.configFixFireExits.Value;
            switch (scene.name)
            {
                case "Level1Experimentation":
                    Plugin.Logger.LogInfo("Detected landing on Experimentation");
                    if (bigMachine != null)
                    {
                        bigMachine.localPosition = new Vector3(-112.04f, bigMachine.localPosition.y, bigMachine.localPosition.z);
                        Plugin.Logger.LogInfo("Fixed factory ambience");
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
                    if (rotateFireExit)
                    {
                        Transform entranceTeleportC = GameObject.Find("/Environment/Teleports/EntranceTeleportC/telePoint")?.transform;
                        if (entranceTeleportC != null)
                        {
                            entranceTeleportC.localRotation = Quaternion.Euler(0f, 180f, 0f);
                            Plugin.Logger.LogInfo("Fixed rotation of external fire exit #2");
                        }
                        Transform entranceTeleportD = GameObject.Find("/Environment/Teleports/EntranceTeleportD/telePoint")?.transform;
                        if (entranceTeleportD != null)
                        {
                            entranceTeleportD.localRotation = Quaternion.Euler(0f, 180f, 0f);
                            Plugin.Logger.LogInfo("Fixed rotation of external fire exit #3");
                        }
                    }
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

            // fire exit #1
            if (rotateFireExit)
            {
                Transform entranceTeleportB = GameObject.Find("/Environment/Teleports/EntranceTeleportB/telePoint")?.transform;
                if (entranceTeleportB != null)
                {
                    entranceTeleportB.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    Plugin.Logger.LogInfo("Fixed rotation of external fire exit #1");
                }
            }
        }
    }
}
