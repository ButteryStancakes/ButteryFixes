using BepInEx.Bootstrap;

namespace ButteryFixes
{
    internal class Compatibility
    {
        internal const string GUID_FAST_CLIMBING = "inoyu.FastClimbing", GUID_BETTER_LADDERS = "e3s1.BetterLadders", GUID_BETTER_STAMINA = "FlipMods.BetterStamina";
        internal const string GUID_LETHAL_FIXES = "uk.1a3.lethalfixes";
        internal const string GUID_GENERAL_IMPROVEMENTS = "ShaosilGaming.GeneralImprovements";
        internal const string GUID_MODEL_REPLACEMENT_API = "meow.ModelReplacementAPI";
        internal const string GUID_BETTER_SPRAY_PAINT = "taffyko.BetterSprayPaint";
        internal const string GUID_EVERYTHING_CAN_DIE = "nwnt.EverythingCanDie";
        internal const string GUID_LETHAL_QUANTITIES = "LethalQuantities";
        internal const string GUID_MORE_COMPANY = "me.swipez.melonloader.morecompany";
        internal const string GUID_TOUCHSCREEN = "me.pm.TheDeadSnake";
        internal const string GUID_REBALANCED_MOONS = "dopadream.lethalcompany.rebalancedmoons";
        internal const string GUID_CRUISER_ADDITIONS = "4902.Cruiser_Additions";
        internal const string GUID_TERMINAL_STUFF = "darmuh.TerminalStuff";

        internal static bool INSTALLED_GENERAL_IMPROVEMENTS, INSTALLED_MORE_COMPANY, INSTALLED_EVERYTHING_CAN_DIE, INSTALLED_LETHAL_QUANTITIES, INSTALLED_REBALANCED_MOONS;
        internal static bool DISABLE_LADDER_PATCH, DISABLE_PLAYERMODEL_PATCHES, DISABLE_SPRAY_PAINT_PATCHES, DISABLE_INTERACT_FIX, DISABLE_PRICE_TEXT_FITTING;

        internal static void Init()
        {
            if (Chainloader.PluginInfos.ContainsKey(GUID_GENERAL_IMPROVEMENTS))
            {
                INSTALLED_GENERAL_IMPROVEMENTS = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - GeneralImprovements detected");
            }

            if (INSTALLED_GENERAL_IMPROVEMENTS || Chainloader.PluginInfos.ContainsKey(GUID_FAST_CLIMBING) || Chainloader.PluginInfos.ContainsKey(GUID_BETTER_LADDERS) || Chainloader.PluginInfos.ContainsKey(GUID_BETTER_STAMINA))
            {
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Ladder patch will be disabled");
                DISABLE_LADDER_PATCH = true;
            }

            if (Chainloader.PluginInfos.ContainsKey(GUID_MODEL_REPLACEMENT_API))
            {
                DISABLE_PLAYERMODEL_PATCHES = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Playermodel patches will be disabled");
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

            if (Chainloader.PluginInfos.ContainsKey(GUID_LETHAL_QUANTITIES))
            {
                INSTALLED_LETHAL_QUANTITIES = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Lethal Quantities detected");
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
        }
    }
}