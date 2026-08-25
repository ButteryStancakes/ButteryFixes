using BepInEx.Configuration;

namespace ButteryFixes
{
    internal enum FilmGrains
    {
        None = -1,
        MenusOnly,
        Full
    }

    internal class Configuration
    {
        static ConfigFile configFile;

        internal static ConfigEntry<bool> makeConductive, fixJumpCheese, showApparatusValue, scanImprovements, fixSurfacePrices, lockInTerminal, filterDecor, typeGordion, playermodelPatches, patchLadders, adjustCooldowns, noBodyNoSignal, theGoldenGoblet, charredBodies, bodiesCollectSelf, cadaverHUD, chemistryFlasks;
        internal static ConfigEntry<FilmGrains> restoreFilmGrain;

        internal static void Init(ConfigFile cfg)
        {
            configFile = cfg;

            CompatibilityConfig();
            GameplayConfig();
            VisualConfig();
            ExtraConfig();
            MigrateLegacyConfigs();
        }

        static void CompatibilityConfig()
        {
            patchLadders = configFile.Bind(
                "Compatibility",
                "PatchLadders",
                true,
                "Fixes stamina being wasted if you hold the sprint button on ladders. This will prevent other mods (Better Ladders, Fast Climbing, Better Stamina, General Improvements, etc.) from allowing ladder sprint as a feature.");

            bodiesCollectSelf = configFile.Bind(
                "Compatibility",
                "BodiesCollectSelf",
                true,
                "Bodies will automatically collect themselves when teleported to the ship, or when players die inside of the ship.");
        }

        static void GameplayConfig()
        {
            makeConductive = configFile.Bind(
                "Gameplay",
                "MakeConductive",
                true,
                "(Host only) Makes some metallic items that are non-conductive in vanilla actually conductive. This fix applies sensibly to the existing items, but you can disable it if you are used to vanilla's properties.");

            fixJumpCheese = configFile.Bind(
                "Gameplay",
                "FixJumpCheese",
                true,
                "(Host only) Enabling this makes enemies hear players jumping and landing on the floor. This fixes the exploit where you can silently move past dogs with sprinting speed by spamming the jump button.");

            fixSurfacePrices = configFile.Bind(
                "Gameplay",
                "FixSurfacePrices",
                true,
                "(Host only) Fixes individual bee hives not having separate prices from one another. (In vanilla, all hives fall into two price classes depending on distance from ship) This also fixes sapsucker eggs having inconsistent prices on identical seeds.");
        }

        static void VisualConfig()
        {
            restoreFilmGrain = configFile.Bind(
                "Visual",
                "RestoreFilmGrain",
                FilmGrains.None,
                "Restores film grain effects from pre-release versions of the game. WARNING: Be aware that this might cause white screens on certain hardware.");

            showApparatusValue = configFile.Bind(
                "Visual",
                "ShowApparatusValue",
                false,
                "Actually show the apparatus' value on the scanner instead of \"???\" (in vanilla, it is always $80)");

            playermodelPatches = configFile.Bind(
                "Visual",
                "PlayermodelPatches",
                true,
                "Fixes some issues with dead bodies not displaying badges, using the wrong suit, not having attachments (bee and bunny), etc.\nIf you use ModelReplacementAPI I would strongly suggest disabling this if you run into issues!");
        }

        static void ExtraConfig()
        {
            scanImprovements = configFile.Bind(
                "Extra",
                "ScanImprovements",
                true,
                "Allows the \"scan\" command on the terminal to count the value of items on your ship while parked at Gordion. Butlers' knives will be visible on the map and \"scan\" command before they are killed. When infected by Cadavers, you will be able to see a red dot when spectating yourself on the ship's radar, just like other players.");

            lockInTerminal = configFile.Bind(
                "Extra",
                "LockInTerminal",
                false,
                "The camera will be frozen when you use the terminal, and typing should be more immediately responsive.\nThis will also lock the camera when charging items, pulling the lever, or sitting in the sofa chair.");

            filterDecor = configFile.Bind(
                "Extra",
                "FilterDecor",
                true,
                "Decorations (suits, furniture, etc.) you have already purchased will be filtered out of the shop's list on the terminal, potentially allowing you to see the unused \"[No items available]\" text.\n(Also fixes the missing bullet points.)");

            typeGordion = configFile.Bind(
                "Extra",
                "TypeGordion",
                false,
                "You can type \"Gordion\" into the terminal (in place of \"company\") when using the route/info commands.");

            adjustCooldowns = configFile.Bind(
                "Extra",
                "AdjustCooldowns",
                true,
                "Changes the cooldown on some items and interactable furniture to prevent sounds overlapping in strange ways.");

            noBodyNoSignal = configFile.Bind(
                "Extra",
                "NoBodyNoSignal",
                false,
                "When a player's corpse is completely destroyed (eaten by a Forest Keeper, suffocated by quicksand, etc.) the radar will display the \"No signal!\" screen instead of spectating the location where they died.");

            theGoldenGoblet = configFile.Bind(
                "Extra",
                "TheGoldenGoblet",
                false,
                "Renames the \"Golden cup\" to \"Golden goblet\"");

            chemistryFlasks = configFile.Bind(
                "Extra",
                "ChemistryFlasks",
                false,
                "Renames the \"Flask\" to \"Chemistry flask\" like the original v45 release poem.");

            charredBodies = configFile.Bind(
                "Extra",
                "CharredBodies",
                true,
                "When a player dies to any sort of explosion, their corpse will appear burnt, much like Cruiser explosions. (This also applies to electrocution from the electric chair.) \"PlayermodelPatches\" is *required* for this to work!");

            cadaverHUD = configFile.Bind(
                "Extra",
                "CadaverHUD",
                false,
                "Displays filter quality in multiplayer, as well as the \"triple threat\" warning when you are about to bloom. This *DOES NOT* display fever warnings or change time-of-death in multiplayer.");
        }

        static void MigrateLegacyConfigs()
        {
            foreach ((string, string) oldKey in new (string, string)[]
            {
                // removed when fixed in v60
                ("Gameplay", "KillOldBirds"),
                // moved to Chameleon
                ("Visual", "FancyEntranceDoors"),
                // overlaps compass as of v70 (also restored in v80)
                ("Visual", "RestoreShipIcon"),
                // moved to Spawn Cycle Fixes
                ("Gameplay", "LimitSpawnChance"),
                ("Gameplay", "UnlimitedOldBirds"),
                ("Gameplay", "MaskHornetsPower"),
                // removed when fixed in v80
                ("Gameplay", "RandomizeDefaultSeed"),
                ("Gameplay", "FixGiantSight"),
                ("Compatibility", "PatchPocketLights"),
                ("Audio", "RestoreArtificeAmbience"),
                ("Visual", "DisableLODFade"),
                // no longer compatible with changes from v80
                ("Compatibility", "AutoCollect"),
                ("Compatibility", "EndOrbitEarly"),
                // replaced with pause menu setting
                ("Visual", "ForceMaxQuality"),
                // removed when fixed in v81
                ("Gameplay", "KeysAreScrap"),
                ("Gameplay", "FixFireExits"),
                // moved to Enemy Sound Fixes
                ("Audio", "MusicDopplerLevel"),
                // just not necessary anymore as of v80 (sorry Sigurd)
                ("Extra", "AlterBestiary"),
            })
            {
                try
                {
                    configFile.Bind(oldKey.Item1, oldKey.Item2, string.Empty, "Legacy setting, doesn't work");
                    configFile.Remove(configFile[oldKey.Item1, oldKey.Item2].Definition);
                }
                catch
                {
                    Plugin.Logger.LogWarning($"Can't delete \"{oldKey.Item1}\" - \"{oldKey.Item2}\" from config");
                }
            }

            // updated to ScanImprovements
            if (!scanImprovements.Value)
            {
                bool scanOnShip = configFile.Bind("Extra", "ScanOnShip", false, "Legacy setting, doesn't work").Value;
                if (scanOnShip)
                    scanImprovements.Value = true;
                configFile.Remove(configFile["Extra", "ScanOnShip"].Definition);
            }
            // updated to FixSurfacePrices
            if (fixSurfacePrices.Value)
            {
                bool fixHivePrices = configFile.Bind("Gameplay", "FixHivePrices", true, "Legacy setting, doesn't work").Value;
                if (!fixHivePrices)
                    fixSurfacePrices.Value = false;
                configFile.Remove(configFile["Gameplay", "FixHivePrices"].Definition);
            }

            configFile.Save();
        }
    }
}
