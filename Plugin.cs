﻿using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using ButteryFixes.Utility;
using HarmonyLib;
using UnityEngine.SceneManagement;

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

    internal enum ScanOnShip
    {
        DontChange = -1,
        Low,
        High
    }

    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("inoyu.FastClimbing", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("e3s1.BetterLadders", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("ShaosilGaming.GeneralImprovements", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Dev1A3.LethalFixes", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("meow.ModelReplacementAPI", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        const string PLUGIN_GUID = "butterystancakes.lethalcompany.butteryfixes", PLUGIN_NAME = "Buttery Fixes", PLUGIN_VERSION = "1.5.2";
        internal static new ManualLogSource Logger;

        internal static bool DISABLE_LADDER_PATCH, ENABLE_SCAN_PATCH, DISABLE_PLAYERMODEL_PATCHES, GENERAL_IMPROVEMENTS, LETHAL_FIXES;

        internal static ConfigEntry<MusicDopplerLevel> configMusicDopplerLevel;
        internal static ConfigEntry<GameResolution> configGameResolution;
        internal static ConfigEntry<bool> configMakeConductive, configMaskHornetsPower, configFixJumpCheese, configKeysAreScrap, configShowApparatusValue, configRandomizeDefaultSeed, configScanOnShip, configFixFireExits;

        void Awake()
        {
            Logger = base.Logger;

            // cross compat stuff
            if (Chainloader.PluginInfos.ContainsKey("inoyu.FastClimbing") || Chainloader.PluginInfos.ContainsKey("e3s1.BetterLadders"))
            {
                Logger.LogInfo("CROSS-COMPATIBILITY - Ladder patch will be disabled");
                DISABLE_LADDER_PATCH = true;
            }

            if (Chainloader.PluginInfos.ContainsKey("ShaosilGaming.GeneralImprovements"))
            {
                GENERAL_IMPROVEMENTS = true;
                Logger.LogInfo("CROSS-COMPATIBILITY - GeneralImprovements detected");
            }

            if (Chainloader.PluginInfos.ContainsKey("Dev1A3.LethalFixes"))
            {
                LETHAL_FIXES = true;
                Logger.LogInfo("CROSS-COMPATIBILITY - LethalFixes detected");
            }

            if (Chainloader.PluginInfos.ContainsKey("meow.ModelReplacementAPI"))
            {
                DISABLE_PLAYERMODEL_PATCHES = true;
                Logger.LogInfo("CROSS-COMPATIBILITY - Playermodel patches will be disabled");
            }

            configGameResolution = Config.Bind(
                "Visual",
                "GameResolution",
                GameResolution.DontChange,
                "The internal resolution rendered by the game. There are unused resolution presets in the game data that you can enable using this option.\n" +
                "\"DontChange\" makes no changes - vanilla is 860x520, but this setting is also compatible with other resolution mods.\n" +
                "\"Low\" is 620x350. \"High\" is 970x580.");

            configShowApparatusValue = Config.Bind(
                "Visual",
                "ShowApparatusValue",
                false,
                "Actually show the apparatus' value on the scanner instead of \"???\" (in vanilla, it is always $80)");

            configMusicDopplerLevel = Config.Bind(
                "Audio",
                "MusicDopplerLevel",
                MusicDopplerLevel.Vanilla,
                "Controls how much Unity's simulated \"Doppler effect\" applies to music sources like the dropship, boombox, etc. (This is what causes pitch distortion when moving towards/away from the source of the music)\n" +
                "\"Vanilla\" makes no changes. \"Reduced\" will make the effect more subtle. \"None\" will disable it completely (so music always plays at the correct pitch)");

            configMakeConductive = Config.Bind(
                "Gameplay",
                "MakeConductive",
                true,
                "(Host only) Makes some metallic items that are non-conductive in vanilla actually conductive. This fix applies sensibly to the existing items, but you can disable it if you are used to vanilla's properties.");

            configMaskHornetsPower = Config.Bind(
                "Gameplay",
                "MaskHornetsPower",
                false,
                "(Host only) Mask hornets internally have the same power level as butlers, but because they spawn in a non-standard way, they don't contribute to the indoor power. Enabling this will prevent additional monsters spawning to replace dead butlers.");

            configFixJumpCheese = Config.Bind(
                "Gameplay",
                "FixJumpCheese",
                true,
                "(Host only) Enabling this makes enemies hear players jumping and landing on the floor. This fixes the exploit where you can silently move past dogs with sprinting speed by spamming the jump button.");

            configKeysAreScrap = Config.Bind(
                "Gameplay",
                "KeysAreScrap",
                false,
                "(Host only) Enabling this will allow you to sell keys for $3 as listed, but will also cause them to be lost if all players die. If this is disabled, they will no longer show \"Value: $3\" on the scanner, instead.");

            configRandomizeDefaultSeed = Config.Bind(
                "Gameplay",
                "RandomizeDefaultSeed",
                true,
                "(Host only) Randomizes the seed when starting a new save file, rather than always using the default of 0. (This changes starting weather and shop sales.)");

            configFixFireExits = Config.Bind(
                "Gameplay",
                "FixFireExits",
                true,
                "Fix fire exit rotation so you are always facing away from the door when you leave. This applies to interiors, as well as the exteriors of the original game's moons.");

            configScanOnShip = Config.Bind(
                "Extra",
                "ScanOnShip",
                false,
                "Allows the \"scan\" command on the terminal to count the number and value of the items on your ship, when in orbit or parked at Gordion.");

            new Harmony(PLUGIN_GUID).PatchAll();

            SceneManager.sceneLoaded += SceneOverrides.OnSceneLoaded;

            Logger.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded");
        }
    }
}