using System.Collections.Generic;
using UnityEngine;

namespace ButteryFixes.Utility
{
    internal static class GlobalReferences
    {
        // Experimentation, Assurance, Vow, Gordion, March, Adamance, Rend, Dine, Offense, Titan, Artifice, Liquidation, Embrion
        internal const int NUM_LEVELS = 13;

        static Terminal _terminal;
        internal static Terminal Terminal
        {
            get
            {
                if (_terminal == null)
                    _terminal = Object.FindAnyObjectByType<Terminal>();

                return _terminal;
            }
        }

        static HangarShipDoor hangarShipDoor;
        internal static HangarShipDoor HangarShipDoor
        {
            get
            {
                if (hangarShipDoor == null)
                    hangarShipDoor = Object.FindAnyObjectByType<HangarShipDoor>();

                return hangarShipDoor;
            }
        }

        internal static Dictionary<string, EnemyType> allEnemiesList = [];

        internal static float dopplerLevelMult = 1f;

        // player corpse burn effect
        internal static Mesh playerBody;
        internal static Material scavengerSuitBurnt;
        internal static bool crashedJetpackAsLocalPlayer;
        internal static GameObject smokeParticle;

        // for making ship node follow the ship
        internal static Transform shipNode;
        internal static Vector3 shipNodeOffset;
        internal static readonly Vector3 shipDefaultPos = new(1.27146339f, 0.278438568f, -7.5f);

        // to fix screen position of scan nodes when changing resolution
        internal static bool patchScanNodes;

        // for end-of-round scrap counter
        internal static int scrapNotCollected = -1, scrapEaten;

        // for cozy lights
        internal static Animator shipAnimator;

        // optimization
        internal static VehicleController vehicleController;

        // lock in terminal
        internal static int lockingCamera;

        // fire exit fix
        internal static bool exitIDsSet;
        internal static bool needToFetchExitPoints;
    }
}
