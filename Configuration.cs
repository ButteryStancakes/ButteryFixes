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

    internal class Configuration
    {
        static ConfigFile configFile;

        internal static ConfigEntry<MusicDopplerLevel> musicDopplerLevel;
        internal static ConfigEntry<GameResolution> gameResolution;
        internal static ConfigEntry<bool> makeConductive, maskHornetsPower, fixJumpCheese, keysAreScrap, showApparatusValue, randomizeDefaultSeed, scanOnShip, fixFireExits, fancyEntranceDoors, killOldBirds, unlimitedOldBirds, restoreShipIcon, limitSpawnChance;

        internal static void Init(ConfigFile cfg)
        {
            configFile = cfg;

            VisualConfig();
            AudioConfig();
            GameplayConfig();
            ExtraConfig();
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

            showApparatusValue = configFile.Bind(
                "Visual",
                "ShowApparatusValue",
                false,
                "Actually show the apparatus' value on the scanner instead of \"???\" (in vanilla, it is always $80)");

            fancyEntranceDoors = configFile.Bind(
                "Visual",
                "FancyEntranceDoors",
                true,
                "Changes the front doors on Rend, Dine, and Artifice to match how they look on the inside of the manor.");

            restoreShipIcon = configFile.Bind(
                "Visual",
                "RestoreShipIcon",
                false,
                "Show the ship icon on the radar (next to the compass) when it is following an outside player. This doesn't display properly in vanilla (bug?)");
        }

        static void AudioConfig()
        {
            musicDopplerLevel = configFile.Bind(
                "Audio",
                "MusicDopplerLevel",
                MusicDopplerLevel.Reduced,
                "Controls how much Unity's simulated \"Doppler effect\" applies to music sources like the dropship, boombox, etc. (This is what causes pitch distortion when moving towards/away from the source of the music)\n" +
                "\"Vanilla\" makes no changes. \"Reduced\" will make the effect more subtle. \"None\" will disable it completely (so music always plays at the correct pitch)");
        }

        static void GameplayConfig()
        {
            makeConductive = configFile.Bind(
                "Gameplay",
                "MakeConductive",
                true,
                "(Host only) Makes some metallic items that are non-conductive in vanilla actually conductive. This fix applies sensibly to the existing items, but you can disable it if you are used to vanilla's properties.");

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

            keysAreScrap = configFile.Bind(
                "Gameplay",
                "KeysAreScrap",
                false,
                "(Host only) Enabling this will allow you to sell keys for $3 as listed, but will also cause them to be lost if all players die. If this is disabled, they will no longer show \"Value: $3\" on the scanner, instead.");

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

            killOldBirds = configFile.Bind(
                "Gameplay",
                "KillOldBirds",
                false,
                "(Host only) Allows Old Birds to be \"killed\" without being destroyed, like in vanilla. This will cause Old Birds to freeze in place when you crash the Cruiser into them at high speeds.");

            unlimitedOldBirds = configFile.Bind(
                "Gameplay",
                "UnlimitedOldBirds",
                false,
                "(Host only) Allows Old Birds to continue spawning even once all the ones presently on the map have \"woken up\", like in vanilla. This will cause them to appear out of nowhere, since they don't have a proper spawning animation.");

            limitSpawnChance = configFile.Bind(
                "Gameplay",
                "LimitSpawnChance",
                false,
                "(Host only) Prevents enemy spawn weight from exceeding 100 (likely the intended maximum) if its spawn curves would normally allow it to do so.\nThis will prevent some enemies from spawning out of control on certain maps.");
        }

        static void ExtraConfig()
        {
            scanOnShip = configFile.Bind(
                "Extra",
                "ScanOnShip",
                false,
                "Allows the \"scan\" command on the terminal to count the number and value of the items on your ship, when in orbit or parked at Gordion.");
        }
    }
}
