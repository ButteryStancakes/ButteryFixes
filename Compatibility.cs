using BepInEx.Bootstrap;
using System.Runtime.InteropServices;

namespace ButteryFixes
{
    internal class Compatibility
    {
        internal const string GUID_FAST_CLIMBING = "inoyu.FastClimbing", GUID_BETTER_LADDERS = "e3s1.BetterLadders";
        internal const string GUID_GENERAL_IMPROVEMENTS = "ShaosilGaming.GeneralImprovements";
        //internal const string LETHAL_FIXES_GUID = "Dev1A3.LethalFixes";
        internal const string GUID_MODEL_REPLACEMENT_API = "meow.ModelReplacementAPI";
        internal const string GUID_STARLANCER_AI_FIX = "AudioKnight.StarlancerAIFix";
        internal const string GUID_BETTER_SPRAY_PAINT = "taffyko.BetterSprayPaint";
        internal const string GUID_EVERYTHING_CAN_DIE = "nwnt.EverythingCanDie";
        internal const string GUID_LETHAL_QUANTITIES = "LethalQuantities";
        //internal const string GUID_MORE_COMPANY = "me.swipez.melonloader.morecompany";
        internal const string GUID_ARTIFICE_BLIZZARD = "butterystancakes.lethalcompany.artificeblizzard";
        internal const string GUID_CELESTIAL_TINT = "CelestialTint";

        internal static bool INSTALLED_GENERAL_IMPROVEMENTS, /*INSTALLED_LETHAL_FIXES,*/ INSTALLED_EVERYTHING_CAN_DIE, INSTALLED_LETHAL_QUANTITIES, /*INSTALLED_MORE_COMPANY,*/ INSTALLED_ARTIFICE_BLIZZARD;
        internal static bool DISABLE_LADDER_PATCH, DISABLE_PLAYERMODEL_PATCHES, DISABLE_ENEMY_MESH_PATCH, DISABLE_SPRAY_PAINT_PATCHES, DISABLE_SUN;

        internal static void Init()
        {
            if (Chainloader.PluginInfos.ContainsKey(GUID_GENERAL_IMPROVEMENTS))
            {
                INSTALLED_GENERAL_IMPROVEMENTS = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - GeneralImprovements detected");
            }

            /*if (Chainloader.PluginInfos.ContainsKey(GUID_LETHAL_FIXES))
            {
                INSTALLED_LETHAL_FIXES = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - LethalFixes detected");
            }*/

            if (Chainloader.PluginInfos.ContainsKey(GUID_FAST_CLIMBING) || Chainloader.PluginInfos.ContainsKey(GUID_BETTER_LADDERS))
            {
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Ladder patch will be disabled");
                DISABLE_LADDER_PATCH = true;
            }

            if (Chainloader.PluginInfos.ContainsKey(GUID_MODEL_REPLACEMENT_API))
            {
                DISABLE_PLAYERMODEL_PATCHES = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Playermodel patches will be disabled");
            }

            if (Chainloader.PluginInfos.ContainsKey(GUID_STARLANCER_AI_FIX))
            {
                DISABLE_ENEMY_MESH_PATCH = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - EnableEnemyMesh patch will be disabled");
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

            /*if (Chainloader.PluginInfos.ContainsKey(GUID_MORE_COMPANY))
            {
                INSTALLED_MORE_COMPANY = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - More Company detected");
            }*/

            if (Chainloader.PluginInfos.ContainsKey(GUID_ARTIFICE_BLIZZARD))
            {
                INSTALLED_ARTIFICE_BLIZZARD = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Artifice Blizzard detected");
            }

            if (Chainloader.PluginInfos.ContainsKey(GUID_CELESTIAL_TINT))
            {
                DISABLE_SUN = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Celestial Tint detected");
            }
        }
    }
}
