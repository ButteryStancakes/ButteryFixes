using BepInEx.Bootstrap;

namespace ButteryFixes
{
    internal class Compatibility
    {
        internal const string FAST_CLIMBING_GUID = "inoyu.FastClimbing", BETTER_LADDERS_GUID = "e3s1.BetterLadders";
        internal const string GENERAL_IMPROVEMENTS_GUID = "ShaosilGaming.GeneralImprovements";
        internal const string LETHAL_FIXES_GUID = "Dev1A3.LethalFixes";
        internal const string MODEL_REPLACEMENT_API_GUID = "meow.ModelReplacementAPI";
        internal const string STARLANCER_AI_FIX_GUID = "AudioKnight.StarlancerAIFix";
        internal const string BETTER_SPRAY_PAINT_GUID = "taffyko.BetterSprayPaint";
        internal const string EVERYTHING_CAN_DIE_GUID = "nwnt.EverythingCanDie";

        internal static bool INSTALLED_GENERAL_IMPROVEMENTS, INSTALLED_LETHAL_FIXES, INSTALLED_EVERYTHING_CAN_DIE;
        internal static bool DISABLE_LADDER_PATCH, DISABLE_PLAYERMODEL_PATCHES, DISABLE_ENEMY_MESH_PATCH, DISABLE_SPRAY_PAINT_PATCHES;

        internal static void Init()
        {
            if (Chainloader.PluginInfos.ContainsKey(GENERAL_IMPROVEMENTS_GUID))
            {
                INSTALLED_GENERAL_IMPROVEMENTS = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - GeneralImprovements detected");
            }

            if (Chainloader.PluginInfos.ContainsKey(LETHAL_FIXES_GUID))
            {
                INSTALLED_LETHAL_FIXES = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - LethalFixes detected");
            }

            if (Chainloader.PluginInfos.ContainsKey(FAST_CLIMBING_GUID) || Chainloader.PluginInfos.ContainsKey(BETTER_LADDERS_GUID))
            {
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Ladder patch will be disabled");
                DISABLE_LADDER_PATCH = true;
            }

            if (Chainloader.PluginInfos.ContainsKey(MODEL_REPLACEMENT_API_GUID))
            {
                DISABLE_PLAYERMODEL_PATCHES = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Playermodel patches will be disabled");
            }

            if (Chainloader.PluginInfos.ContainsKey(STARLANCER_AI_FIX_GUID))
            {
                DISABLE_ENEMY_MESH_PATCH = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - EnableEnemyMesh patch will be disabled");
            }

            if (Chainloader.PluginInfos.ContainsKey(BETTER_SPRAY_PAINT_GUID))
            {
                DISABLE_SPRAY_PAINT_PATCHES = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Spray paint patches will be disabled");
            }

            if (Chainloader.PluginInfos.ContainsKey(EVERYTHING_CAN_DIE_GUID))
            {
                INSTALLED_EVERYTHING_CAN_DIE = true;
                Plugin.Logger.LogInfo("CROSS-COMPATIBILITY - Everything Can Die detected");
            }
        }
    }
}
