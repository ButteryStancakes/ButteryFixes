using BepInEx.Configuration;

namespace ButteryFixes
{
    internal enum MusicDopplerLevel
    {
        Vanilla = -1,
        None,
        Reduced
    }

    internal enum GameResolution
    {
        DontChange = -1,
        Low,
        High
    }

    internal enum FilmGrains
    {
        None = -1,
        MenusOnly,
        Full
    }

    internal class Configuration
    {
        static ConfigFile configFile;

        internal static ConfigEntry<MusicDopplerLevel> musicDopplerLevel;
        internal static ConfigEntry<GameResolution> gameResolution;
        internal static ConfigEntry<bool> makeConductive, maskHornetsPower, fixJumpCheese, keysAreScrap, showApparatusValue, randomizeDefaultSeed, scanImprovements, fixFireExits, unlimitedOldBirds, limitSpawnChance, fixHivePrices, lockInTerminal, filterDecor, fixGiantSight, typeGordion, restoreArtificeAmbience, disableLODFade, playermodelPatches;
        internal static ConfigEntry<FilmGrains> restoreFilmGrain;

        internal static void Init(ConfigFile cfg)
        {
            configFile = cfg;

            GameplayConfig();
            VisualConfig();
            AudioConfig();
            ExtraConfig();
            MigrateLegacyConfigs();
        }

        static void GameplayConfig()
        {
            randomizeDefaultSeed = configFile.Bind(
                "Gameplay",
                "RandomizeDefaultSeed",
                true,
                "(Host only) Randomizes the seed when starting a new save file, rather than always using the default of 0. (This changes starting weather and shop sales.)");

            fixFireExits = configFile.Bind(
                "Gameplay",
                "FixFireExits",
                true,
                "Fix fire exit rotation so you are always facing away from the door when you leave. This applies to interiors, as well as the exteriors of the original game's moons.");

            makeConductive = configFile.Bind(
                "Gameplay",
                "MakeConductive",
                true,
                "(Host only) Makes some metallic items that are non-conductive in vanilla actually conductive. This fix applies sensibly to the existing items, but you can disable it if you are used to vanilla's properties.");

            keysAreScrap = configFile.Bind(
                "Gameplay",
                "KeysAreScrap",
                false,
                "(Host only) Enabling this will allow you to sell keys for $3 as listed, but will also cause them to be lost if all players die. If this is disabled, they will no longer show \"Value: $3\" on the scanner, instead.");

            limitSpawnChance = configFile.Bind(
                "Gameplay",
                "LimitSpawnChance",
                true,
                "(Host only) Prevents enemy spawn weight from exceeding 100 (likely the intended maximum) if its spawn curves would normally allow it to do so.\nThis will prevent some enemy types from spawning out of control on certain maps.");

            unlimitedOldBirds = configFile.Bind(
                "Gameplay",
                "UnlimitedOldBirds",
                false,
                "(Host only) Allows Old Birds to continue spawning even once all the ones presently on the map have \"woken up\", like in vanilla. This will cause them to appear out of nowhere, since they don't have a proper spawning animation\nThis will also allow outdoor spawns to \"overflow\" when you unplug the apparatus, since that doesn't add Old Birds to the power count in vanilla.");

            maskHornetsPower = configFile.Bind(
                "Gameplay",
                "MaskHornetsPower",
                false,
                "(Host only) Mask hornets internally have the same power level as butlers, but because they spawn in a non-standard way, they don't contribute to the indoor power. Enabling this will prevent additional monsters spawning to replace dead butlers.");

            fixJumpCheese = configFile.Bind(
                "Gameplay",
                "FixJumpCheese",
                true,
                "(Host only) Enabling this makes enemies hear players jumping and landing on the floor. This fixes the exploit where you can silently move past dogs with sprinting speed by spamming the jump button.");

            fixHivePrices = configFile.Bind(
                "Gameplay",
                "FixHivePrices",
                true,
                "(Host only) Fixes individual bee hives not having separate prices from one another. (In vanilla, all hives fall into two price classes depending on distance from ship)");

            fixGiantSight = configFile.Bind(
                "Gameplay",
                "FixGiantSight",
                true,
                "(Host only) Fix Forest Keepers permanently remembering players and instantly entering chase on-sight. (Their memory is meant to decay when those players are not visible, but due to a logical error, it can never decrease unless at least one other player is being observed by the giant.)");
        }

        static void VisualConfig()
        {
            gameResolution = configFile.Bind(
                "Visual",
                "GameResolution",
                GameResolution.DontChange,
                "The internal resolution rendered by the game. There are unused resolution presets in the game data that you can enable using this option.\n" +
                "\"DontChange\" makes no changes - vanilla is 860x520, but this setting is also compatible with other resolution mods.\n" +
                "\"Low\" is 620x350. \"High\" is 970x580.");

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

            disableLODFade = configFile.Bind(
                "Visual",
                "DisableLODFade",
                true,
                "Disables level-of-detail cross-fading, which is broken in vanilla. This does not prevent \"pop in\" (when moving towards/away from objects) from occurring, but makes it happen only once instead of twice.");

            playermodelPatches = configFile.Bind(
                "Visual",
                "PlayermodelPatches",
                true,
                "Fixes some issues with dead bodies not displaying badges, using the wrong suit, not having attachments (bee and bunny), etc. Also burns corpses when killed by explosions.\nIf you use ModelReplacementAPI I would strongly suggest disabling this if you run into issues!");
        }

        static void AudioConfig()
        {
            musicDopplerLevel = configFile.Bind(
                "Audio",
                "MusicDopplerLevel",
                MusicDopplerLevel.Reduced,
                "Controls how much Unity's simulated \"Doppler effect\" applies to music sources like the dropship, boombox, etc. (This is what causes pitch distortion when moving towards/away from the source of the music)\n" +
                "\"Vanilla\" makes no changes. \"Reduced\" will make the effect more subtle. \"None\" will disable it completely (so music always plays at the correct pitch)");

            restoreArtificeAmbience = configFile.Bind(
                "Audio",
                "RestoreArtificeAmbience",
                true,
                "Restores the unique night-time ambience on Artifice, which is broken and doesn't play in vanilla.");
        }

        static void ExtraConfig()
        {
            scanImprovements = configFile.Bind(
                "Extra",
                "ScanImprovements",
                false,
                "Allows the \"scan\" command on the terminal to count the value of items on your ship in orbit. Butlers' knives will be visible on the map and \"scan\" command before they are killed.");

            lockInTerminal = configFile.Bind(
                "Extra",
                "LockInTerminal",
                false,
                "The camera will be frozen when you use the terminal, and typing should be more immediately responsive.\nThis will also lock the camera when charging items, pulling the lever, or sitting in the sofa chair.");

            filterDecor = configFile.Bind(
                "Extra",
                "FilterDecor",
                false,
                "Decorations (suits, furniture, etc.) you have already purchased will be filtered out of the shop's list on the terminal, potentially allowing you to see the unused \"[No items available]\" text.");

            typeGordion = configFile.Bind(
                "Extra",
                "TypeGordion",
                false,
                "You can type \"Gordion\" into the terminal (in place of \"company\") when using the route/info commands.");
        }

        static void MigrateLegacyConfigs()
        {
            // removed when fixed in v60
            configFile.Bind("Gameplay", "KillOldBirds", true, "Legacy setting, doesn't work");
            configFile.Remove(configFile["Gameplay", "KillOldBirds"].Definition);
            // moved to Chameleon
            configFile.Bind("Visual", "FancyEntranceDoors", false, "Legacy setting, use \"Chameleon\" instead");
            configFile.Remove(configFile["Visual", "FancyEntranceDoors"].Definition);
            // updated to ScanImprovements
            if (!scanImprovements.Value)
            {
                bool scanOnShip = configFile.Bind("Extra", "ScanOnShip", false, "Legacy setting, doesn't work").Value;
                if (scanOnShip)
                    scanImprovements.Value = true;
                configFile.Remove(configFile["Extra", "ScanOnShip"].Definition);
            }
            // overlaps compass as of v70
            configFile.Bind("Visual", "RestoreShipIcon", true, "Legacy setting, doesn't work");
            configFile.Remove(configFile["Visual", "RestoreShipIcon"].Definition);

            configFile.Save();
        }
    }
}
