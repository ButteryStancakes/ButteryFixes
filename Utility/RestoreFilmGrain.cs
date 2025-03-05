using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace ButteryFixes.Utility
{
    internal class RestoreFilmGrain
    {
        static Texture scanline, scanlineBlue;

        internal static bool GetTextures()
        {
            try
            {
                AssetBundle scanlines = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "filmgrain"));
                scanline = scanlines.LoadAsset<Texture>("scanline");
                scanlineBlue = scanlines.LoadAsset<Texture>("scanlineBlue");
                scanlines.Unload(false);
                return true;
            }
            catch
            {
                Plugin.Logger.LogError("Encountered some error loading assets from bundle \"filmgrain\". Did you install the plugin correctly?");
                return false;
            }
        }

        internal static void OverrideVolumes(Scene scene, LoadSceneMode mode)
        {
            if (Configuration.restoreFilmGrain.Value == FilmGrains.None || scanline == null || scanlineBlue == null)
                return;

            foreach (Volume volume in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (volume.sharedProfile.name != "UIEffects" && Configuration.restoreFilmGrain.Value < FilmGrains.NotRadar)
                    continue;

                switch (volume.sharedProfile.name)
                {
                    case "UIEffects":
                        ApplyFilmGrain(volume.sharedProfile, 0.085f, 0.06f, scanline);

                        volume.sharedProfile.TryGet(out Bloom bloom);
                        if (bloom != null)
                        {
                            bloom.active = true;
                            Plugin.Logger.LogDebug($"Bloom: {volume.sharedProfile.name}");
                        }
                        break;
                    case "IngameHUD":
                        ApplyFilmGrain(volume.sharedProfile, 0f, 0.49f, scanline);
                        break;
                    case "FlashbangedFilter":
                        ApplyFilmGrain(volume.sharedProfile, 1f, 0.818f, type: FilmGrainLookup.Large02);
                        break;
                    case "InsanityVolume":
                        ApplyFilmGrain(volume.sharedProfile, 0.374f, 0.558f, type: FilmGrainLookup.Large02);
                        break;
                    case "RadarCameraVolume 1":
                        //case "SecurityCameraVolume":
                        if (Configuration.restoreFilmGrain.Value == FilmGrains.Full)
                            ApplyFilmGrain(volume.sharedProfile, 0.125f, 1f, scanline);
                        break;
                    case "WakeUpVolume":
                        ApplyFilmGrain(volume.sharedProfile, 0.112f, 0.524f, scanline);
                        break;
                    case "ScanVolume":
                        ApplyFilmGrain(volume.sharedProfile, 0.073f, 0.49f, scanlineBlue);
                        break;
                }
            }
        }

        static void ApplyFilmGrain(VolumeProfile profile, float intensity, float response, Texture texture = null, FilmGrainLookup type = FilmGrainLookup.Custom)
        {
            profile.TryGet(out FilmGrain filmGrain);

            if (filmGrain == null)
                filmGrain = profile.Add<FilmGrain>();

            if (texture != null)
            {
                filmGrain.texture.Override(texture);
                filmGrain.type.Override(FilmGrainLookup.Custom);
            }
            else
                filmGrain.type.Override(type);

            filmGrain.intensity.Override(intensity);
            filmGrain.response.Override(response);

            // for parity
            if (profile.name == "InsanityVolume")
                filmGrain.response.overrideState = false;

            Plugin.Logger.LogDebug($"Film grain: {profile.name}");
        }
    }
}
