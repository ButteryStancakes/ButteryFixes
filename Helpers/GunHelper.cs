using System.Collections.Generic;
using UnityEngine;

namespace ButteryFixes.Helpers
{
    static class GunHelper
    {
        public static void FilterDuplicateHits(ref int num, ref RaycastHit[] results)
        {
            int index = 0;
            List<EnemyAI> enemies = new();

            for (int i = 0; i < num; i++)
            {
                if (results[i].transform.TryGetComponent(out EnemyAICollisionDetect enemyCollider))
                {
                    if (enemyCollider.onlyCollideWhenGrounded)
                        Plugin.Logger.LogInfo($"Filtered shotgun hit on \"{enemyCollider.mainScript.name}\" (wouldn't deal damage)");
                    else if (!enemies.Contains(enemyCollider.mainScript))
                    {
                        enemies.Add(enemyCollider.mainScript);
                        if (i > index)
                            results[index] = results[i];
                        index++;
                    }
                    else
                        Plugin.Logger.LogInfo($"Filtered duplicate shotgun hit on \"{enemyCollider.mainScript.name}\"");
                }
            }

            num = index;
        }
    }
}
