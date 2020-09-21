using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BS_Utils.Utilities;
using IPA;
using SongCore;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;

namespace SongDataCleaner
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static IPALogger Log { get; private set; }
        
        private static SongDataCleaner _songDataCleaner;
        private static string Name => "SongDataCleaner";
        
        private static bool _didRun;

        [Init]
        public void Init(IPALogger logger)
        {
            Log = logger;
            Log.Info("plugin initialized");
        }

        [OnStart]
        public void OnStart()
        {
            BSEvents.OnLoad();
            _songDataCleaner = new GameObject(Name).AddComponent<SongDataCleaner>();
        }

        [OnEnable]
        public void Enable()
        {
            BSEvents.levelSelected += OnLevelSelected;
            Loader.SongsLoadedEvent += OnSongsLoaded;
            PluginUI.instance.Setup();
        }

        [OnDisable]
        [OnExit]
        public void Disable()
        {
            BSEvents.levelSelected -= OnLevelSelected;
            Loader.SongsLoadedEvent -= OnSongsLoaded;
        }
        
        private void OnLevelSelected(LevelCollectionViewController levelCollectionViewController, IPreviewBeatmapLevel previewBeatmapLevel)
        {
            if (Loader.AreSongsLoading)
            {
                return;
            }
            
            _songDataCleaner.Run(new List<CustomPreviewBeatmapLevel>
            {
                previewBeatmapLevel as CustomPreviewBeatmapLevel
            }, false);
        }


        private void OnSongsLoaded(Loader loader, Dictionary<string, CustomPreviewBeatmapLevel> levelDictionary)
        {
            if (_didRun || Loader.AreSongsLoading)
            {
                return;
            }

            _didRun = true;
            Log.Info("SongCore finished loading, cleaning infos");
            _songDataCleaner.Run(levelDictionary.Values.ToList());
        }
    }
}