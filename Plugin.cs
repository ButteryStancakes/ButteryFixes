using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

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

    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("inoyu.FastClimbing", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("e3s1.BetterLadders", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("ShaosilGaming.GeneralImprovements", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        const string PLUGIN_GUID = "butterystancakes.lethalcompany.butteryfixes", PLUGIN_NAME = "Buttery Fixes", PLUGIN_VERSION = "1.4.0";
        internal static new ManualLogSource Logger;

        internal static bool DISABLE_LADDER_PATCH, ENABLE_SCAN_PATCH, GENERAL_IMPROVEMENTS;

        internal static ConfigEntry<MusicDopplerLevel> configMusicDopplerLevel;
        internal static ConfigEntry<GameResolution> configGameResolution;
        internal static ConfigEntry<bool> configMakeConductive, configMaskHornetsPower, configFixJumpCheese, configKeysAreScrap;

        void Awake()
        {
            Logger = base.Logger;

            if (Chainloader.PluginInfos.ContainsKey("inoyu.FastClimbing") || Chainloader.PluginInfos.ContainsKey("e3s1.BetterLadders"))
            {
                Logger.LogInfo("CROSS-COMPATIBILITY - Ladder patch will be disabled");
                DISABLE_LADDER_PATCH = true;
            }

            GENERAL_IMPROVEMENTS = Chainloader.PluginInfos.ContainsKey("ShaosilGaming.GeneralImprovements");

            configGameResolution = Config.Bind(
                "Visual",
                "GameResolution",
                GameResolution.DontChange,
                "The internal resolution rendered by the game. There are unused resolution presets in the game data that you can enable using this option.\n" +
                "\"DontChange\" makes no changes - vanilla is 860x520, but this setting is also compatible with other resolution mods.\n" +
                "\"Low\" is 620x350. \"High\" is 970x580.");

            configMusicDopplerLevel = Config.Bind(
                "Audio",
                "MusicDopplerLevel",
                MusicDopplerLevel.Vanilla,
                "Controls how much Unity's simulated \"doppler effect\" applies to music sources like the dropship, boombox, etc. (This is what causes pitch distortion when moving towards/away from the source of the music)\n" +
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

            new Harmony(PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded");
        }
    }
}