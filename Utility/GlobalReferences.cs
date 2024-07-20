using System.Collections.Generic;
using UnityEngine;

namespace ButteryFixes.Utility
{
    internal class GlobalReferences
    {
        internal static Dictionary<string, EnemyType> allEnemiesList = [];

        internal static float dopplerLevelMult = 1f;

        // tragedy mimic patches
        internal static Mesh tragedyMask, tragedyMaskLOD, tragedyMaskEyesFilled;
        internal static Material tragedyMaskMat;
        internal static AudioClip[] tragedyMaskRandomClips;

        // player corpse burn effect
        internal static Mesh playerBody;
        internal static Material scavengerSuitBurnt;
        internal static bool crashedJetpackAsLocalPlayer;

        // for making ship node follow the ship
        internal static Transform shipNode;
        internal static Vector3 shipNodeOffset;
        internal static readonly Vector3 shipDefaultPos = new(1.27146339f, 0.278438568f, -7.5f);

        // to fix screen position of scan nodes when changing resolution
        internal static bool patchScanNodes;

        // for end-of-round scrap counter
        internal static int scrapNotCollected;
    }
}
