using BepInEx.Bootstrap;

namespace ButteryFixes
{
    internal class Compatibility
    {
        internal const string GUID_LETHAL_FIXES = "uk.1a3.lethalfixes",
                              GUID_GENERAL_IMPROVEMENTS = "ShaosilGaming.GeneralImprovements",
                              GUID_BETTER_SPRAY_PAINT = "taffyko.BetterSprayPaint",
                              GUID_EVERYTHING_CAN_DIE = "nwnt.EverythingCanDie",
                              GUID_MORE_COMPANY = "me.swipez.melonloader.morecompany",
                              GUID_TOUCHSCREEN = "me.pm.TheDeadSnake",
                              GUID_REBALANCED_MOONS = "dopadream.lethalcompany.rebalancedmoons",
                              GUID_CRUISER_ADDITIONS = "4902.Cruiser_Additions",
                              GUID_TERMINAL_STUFF = "darmuh.TerminalStuff",
                              GUID_LOBBY_COMPATIBILITY = "BMX.LobbyCompatibility",
                           // GUID_OPEN_BODY_CAMS = "Zaggy1024.OpenBodyCams",
                              GUID_NO_LOST_SIGNAL = "Tomatobird.NoLostSignal",
                              GUID_DROP_SHIP_DELIVERY_CAP_MODIFIER = "com.github.Sylkadi.DropShipDeliveryCapModifier",
                              GUID_UPTURNED_VARIETY = "butterystancakes.lethalcompany.upturnedvariety",
                              GUID_VERSION55_COMPANY_CRUISER = "scandal.v55cruiser",
                              GUID_V70_POWERED_LIGHTS_FIX = "watergun.v72lightfix",
                              GUID_CUSTOM_RESOLUTION = "Tomatobird.CustomResolution",
                              GUID_SCIENCE_BIRD_TWEAKS = "ScienceBird.ScienceBirdTweaks";

        internal static bool INSTALLED_GENERAL_IMPROVEMENTS,
                             INSTALLED_MORE_COMPANY,
                             INSTALLED_EVERYTHING_CAN_DIE,
                             INSTALLED_REBALANCED_MOONS,
                             INSTALLED_UPTURNED_VARIETY,
                             INSTALLED_V55_CRUISER,
                          // INSTALLED_OPEN_BODY_CAMS,
                             DISABLE_SPRAY_PAINT_PATCHES,
                             DISABLE_INTERACT_FIX,
                             DISABLE_PRICE_TEXT_FITTING,
                          // DISABLE_ROTATION_PATCH,
                             DISABLE_SIGNAL_PATCH,
                             DISABLE_PURCHASE_CAP_PATCH,
                             DISABLE_RESOLUTION_PATCHES;

        internal static void Init()
        {
            if (Chainloader.PluginInfos.ContainsKey(GUID_GENERAL_IMPROVEMENTS))
            {
                INSTALLED_GENERAL_IMPROVEMENTS = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - GeneralImprovements detected");
            }

            if (Chainloader.PluginInfos.ContainsKey(GUID_BETTER_SPRAY_PAINT))
            {
                DISABLE_SPRAY_PAINT_PATCHES = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Spray paint patches will be disabled");
            }

            if (Chainloader.PluginInfos.ContainsKey(GUID_EVERYTHING_CAN_DIE))
            {
                INSTALLED_EVERYTHING_CAN_DIE = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Everything Can Die detected");
            }

            if (Chainloader.PluginInfos.ContainsKey(GUID_MORE_COMPANY))
            {
                INSTALLED_MORE_COMPANY = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - More Company detected");
            }

            if (Chainloader.PluginInfos.ContainsKey(GUID_TOUCHSCREEN))
            {
                DISABLE_INTERACT_FIX = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Touchscreen detected");
            }

            if (Chainloader.PluginInfos.ContainsKey(GUID_REBALANCED_MOONS))
            {
                INSTALLED_REBALANCED_MOONS = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Rebalanced Moons detected");
            }

            if (INSTALLED_GENERAL_IMPROVEMENTS || Chainloader.PluginInfos.ContainsKey(GUID_TERMINAL_STUFF))
            {
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Price text patch will be disabled");
                DISABLE_PRICE_TEXT_FITTING = true;
            }

            if (Chainloader.PluginInfos.ContainsKey(GUID_LOBBY_COMPATIBILITY))
            {
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Lobby Compatibility detected");
                LobbyCompatibility.Init();
            }

            /*if (Chainloader.PluginInfos.ContainsKey(GUID_OPEN_BODY_CAMS))
            {
                INSTALLED_OPEN_BODY_CAMS = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - OpenBodyCams detected");
            }*/

            if (Chainloader.PluginInfos.ContainsKey(GUID_NO_LOST_SIGNAL))
            {
                DISABLE_SIGNAL_PATCH = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - NoLostSignal detected");
            }

            if (INSTALLED_GENERAL_IMPROVEMENTS || Chainloader.PluginInfos.ContainsKey(GUID_DROP_SHIP_DELIVERY_CAP_MODIFIER))
            {
                DISABLE_PURCHASE_CAP_PATCH = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Purchase cap patch (x12) will be disabled");
            }

            if (Chainloader.PluginInfos.ContainsKey(GUID_UPTURNED_VARIETY))
            {
                INSTALLED_UPTURNED_VARIETY = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Upturned Variety detected");
            }

            if (Chainloader.PluginInfos.ContainsKey(GUID_VERSION55_COMPANY_CRUISER))
            {
                INSTALLED_V55_CRUISER = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Version-55 Company Cruiser detected");
            }

            if (Chainloader.PluginInfos.ContainsKey(GUID_CUSTOM_RESOLUTION))
            {
                DISABLE_RESOLUTION_PATCHES = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - CustomResolution detected");
            }
        }
    }
}