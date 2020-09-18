using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BS_Utils.Utilities;
using IPA;
using SongCore;
using IPALogger = IPA.Logging.Logger;

namespace SongDataCleaner
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        private static IPALogger Log { get; set; }
        private static bool _didCompleteRun;

        [Init]
        public void Init(IPALogger logger)
        {
            Log = logger;
            Log.Info("plugin initialized");
        }

        [OnStart]
        public void OnApplicationStart()
        {
            BSEvents.OnLoad();
            BSEvents.levelSelected += OnLevelSelected;
            Loader.SongsLoadedEvent += OnSongsLoaded;
        }

        private void OnLevelSelected(LevelCollectionViewController levelCollectionViewController, IPreviewBeatmapLevel previewBeatmapLevel)
        {
            CleanLevelData(previewBeatmapLevel as CustomPreviewBeatmapLevel);
        }

        private void OnSongsLoaded(Loader loader, Dictionary<string, CustomPreviewBeatmapLevel> levelDictionary)
        {
            Log.Debug("SongCore finished loading, cleaning directories");
            
            Parallel.ForEach(levelDictionary, pair => CleanLevelData(pair.Value));
            
            if (_didCompleteRun)
            {
                return;
            }
            
            Loader.Instance.RefreshSongs();
            _didCompleteRun = true;
        }

        private void CleanLevelData(CustomPreviewBeatmapLevel level)
        {
            if (level == null)
            {
                Log.Warn("level is null");
                return;
            }

            Log.Debug($"Cleaning {level.customLevelPath}");
        }
    }
}