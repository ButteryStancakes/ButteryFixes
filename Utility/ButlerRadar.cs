using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ButteryFixes.Utility
{
    internal class ButlerRadar
    {
        internal class RadarButler
        {
            internal ButlerEnemyAI butler;
            internal Transform radarIcon, knife;
            internal bool dying;
        }

        static List<RadarButler> allButlers = new(7);

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

        internal static void ClearAllButlers()
        {
            if (allButlers.Count < 1)
                return;

            for (int i = allButlers.Count - 1; i >= 0; i--)
            {
                if (allButlers[i]?.radarIcon != null)
                    Object.Destroy(allButlers[i].radarIcon);
            }
            allButlers.Clear();

            Plugin.Logger.LogDebug($"Radar: No more butlers");
        }

        internal static void UpdateButlers()
        {
            if (allButlers.Count < 1)
                return;

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
    }
}
