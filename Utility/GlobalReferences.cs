using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;

namespace ButteryFixes.Utility
{
    internal static class GlobalReferences
    {
        // Experimentation, Assurance, Vow, Gordion, March, Adamance, Rend, Dine, Offense, Titan, Artifice, Liquidation, Embrion
        internal const int NUM_LEVELS = 13;

        internal static readonly string[] INTERIOR_WHITELIST = [
            "Level1Flow",
            "Level1Flow3Exits",
            "Level2Flow",
            "Level3Flow"
        ];

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

        /*static HangarShipDoor hangarShipDoor;
        internal static HangarShipDoor HangarShipDoor
        {
            get
            {
                if (hangarShipDoor == null)
                    hangarShipDoor = Object.FindAnyObjectByType<HangarShipDoor>();

                return hangarShipDoor;
            }
        }*/

        static StartMatchLever startMatchLever;
        internal static StartMatchLever StartMatchLever
        {
            get
            {
                if (startMatchLever == null)
                    startMatchLever = Object.FindAnyObjectByType<StartMatchLever>();

                return startMatchLever;
            }
        }

        static MoldSpreadManager moldSpreadManager;
        internal static MoldSpreadManager MoldSpreadManager
        {
            get
            {
                if (moldSpreadManager == null)
                    moldSpreadManager = Object.FindAnyObjectByType<MoldSpreadManager>();

                return moldSpreadManager;
            }
        }

        internal static Dictionary<string, EnemyType> allEnemiesList = [];

        // player corpse burn effect
        internal static Mesh playerBody;
        internal static Material scavengerSuitBurnt;
        internal static bool crashedJetpackAsLocalPlayer;
        internal static GameObject smokeParticle;

        // for making ship node follow the ship
        internal static Transform shipNode;
        internal static Vector3 shipNodeOffset;
        internal static readonly Vector3 shipDefaultPos = new(1.27146339f, 0.278438568f, -7.5f);

        // for end-of-round scrap counter
        internal static int scrapNotCollected = -1, scrapEaten;

        // for cozy lights
        internal static Animator shipAnimator;

        // optimization
        internal static VehicleController vehicleController;

        // lock in terminal
        internal static int lockingCamera;
        internal static bool sittingInArmchair;

        // new deaths from v70
        internal static PlayerControllerB friedPlayer, gibbedPlayer;

        // radar
        internal static List<Bounds> caveTiles = [];
        internal static Vector3 mainEntrancePos;
        internal static Bounds mineStartBounds;

        // arms visibility fix
        internal static Renderer viewmodelArms;
        internal static Camera shipCamera, securityCamera;

        // microwave rework
        internal static List<GrabbableObject> microwavedItems = [];

        // profitability
        internal static PlayerControllerB lastDriver;

        internal static Vector3 lastLightningStrike = new(3000f, 0f, 3000f);
        internal static float lightningLastStruck;
    }
}
