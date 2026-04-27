using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ButteryFixes.Utility
{
    internal static class EnemyRadar
    {
        internal class RadarButler
        {
            internal ButlerEnemyAI butler;
            internal Transform radarIcon, knife;
            internal bool dying;
        }

        static List<RadarButler> allButlers = new(7);
        static Transform infectionDot;

        internal static void SpawnButler(ButlerEnemyAI butler)
        {
            allButlers.Add(new()
            {
                butler = butler,
                radarIcon = Object.Instantiate(StartOfRound.Instance.itemRadarIconPrefab, RoundManager.Instance.mapPropsContainer?.transform).transform,
                knife = butler.creatureAnimator.transform.Find("Rig 1/Arms/RightArm/upperRightArmContainer/upper_arm.R_target/Knife")
            });
            Plugin.Logger.LogDebug($"Radar: Butler #{butler.GetInstanceID()} registered");
        }

        internal static void Reset()
        {
            if (allButlers.Count > 0)
            {
                for (int i = allButlers.Count - 1; i >= 0; i--)
                {
                    if (allButlers[i]?.radarIcon != null)
                        Object.Destroy(allButlers[i].radarIcon.gameObject);
                }
                allButlers.Clear();

                Plugin.Logger.LogDebug($"Radar: No more butlers");
            }

            CureLocalPlayer();
        }

        internal static void Refresh()
        {
            if (allButlers.Count > 0)
            {
                for (int i = allButlers.Count - 1; i >= 0; i--)
                {
                    if (allButlers[i] == null || allButlers[i].radarIcon == null)
                    {
                        allButlers.RemoveAt(i);
                        Plugin.Logger.LogDebug($"Radar: Butler at index {i} is missing, this shouldn't happen");
                        continue;
                    }

                    if (allButlers[i].butler.isEnemyDead)
                    {
                        if (!allButlers[i].dying)
                        {
                            allButlers[i].dying = true;
                            StartOfRound.Instance.StartCoroutine(ButlerDeathAnimation(allButlers[i]));
                            allButlers[i].radarIcon.position = allButlers[i].butler.transform.position + (Vector3.up * 0.5f);
                            Plugin.Logger.LogDebug($"Radar: Butler #{allButlers[i].butler.GetInstanceID()} just died");
                        }
                        continue;
                    }

                    Vector3 knifePos = allButlers[i].knife.position;
                    if (allButlers[i].knife.localScale.x < 0.025f)
                    {
                        knifePos.x = allButlers[i].butler.transform.position.x;
                        knifePos.z = allButlers[i].butler.transform.position.z;
                    }
                    allButlers[i].radarIcon.position = knifePos;
                }
            }
        }

        static IEnumerator ButlerDeathAnimation(RadarButler butler)
        {
            yield return new WaitForSeconds(1.1f);

            if (butler == null)
                yield break;

            while (butler != null && !butler.butler.creatureAnimator.GetBool("popFinish"))
                yield return null;

            if (butler != null)
            {
                if (butler.radarIcon != null)
                    Object.Destroy(butler.radarIcon.gameObject);
                allButlers.Remove(butler);
                Plugin.Logger.LogDebug($"Radar: Butler #{butler.butler.GetInstanceID()} dropped knife");
            }

            yield break;
        }

        internal static int CountButlers()
        {
            return allButlers.Count;
        }

        internal static void InfectLocalPlayer(CadaverGrowthAI cadaverGrowthAI)
        {
            if (infectionDot == null)
            {
                if (cadaverGrowthAI?.scanNodePrefab == null)
                    return;

                infectionDot = Object.Instantiate(cadaverGrowthAI.scanNodePrefab.transform.Find("MapDot (2)"), GameNetworkManager.Instance.localPlayerController.bodyParts[5]);
                infectionDot.SetLocalPositionAndRotation(new(0f, 0.231123909f, -0.00785285234f), Quaternion.Euler(0f, 99.461f, 0f));
                infectionDot.localScale = new(0.215845078f, 0.208411828f, 0.211130708f);
            }

            if (!infectionDot.gameObject.activeSelf)
            {
                infectionDot.gameObject.SetActive(true);
                Plugin.Logger.LogDebug("Radar: Local player is infected");
            }
        }

        internal static void CureLocalPlayer()
        {
            if (infectionDot != null && infectionDot.gameObject.activeSelf)
            {
                infectionDot.gameObject.SetActive(false);
                Plugin.Logger.LogDebug("Radar: Infection is over");
            }
        }
    }
}
