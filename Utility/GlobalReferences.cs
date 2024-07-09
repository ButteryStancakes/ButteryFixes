using System.Collections.Generic;
using UnityEngine;

namespace ButteryFixes.Utility
{
    internal class GlobalReferences
    {
        internal static Dictionary<string, EnemyType> allEnemiesList = [];
        internal static float dopplerLevelMult = 1f;

        internal static Mesh tragedyMask, tragedyMaskLOD, tragedyMaskEyesFilled;
        internal static Material tragedyMaskMat;
        internal static AudioClip[] tragedyMaskRandomClips;

        internal static Mesh playerBody;
        internal static Material scavengerSuitBurnt;

        internal static bool crashedJetpackAsLocalPlayer;
    }
}
