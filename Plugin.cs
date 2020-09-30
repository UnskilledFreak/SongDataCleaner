using System.Collections.Generic;
using System.Linq;
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
        private static bool _firstRunDone;
        private static string Name => "SongDataCleaner";

        [Init]
        public void Init(IPALogger logger)
        {
            Log = logger;
            Log.Info("plugin initialized");
        }

        [OnStart]
        public void OnStart()
        {
            _songDataCleaner = new GameObject(Name).AddComponent<SongDataCleaner>();
        }

        [OnEnable]
        public void Enable()
        {
            Loader.SongsLoadedEvent += OnSongsLoaded;
            PluginUI.instance.Setup();
        }

        [OnDisable]
        [OnExit]
        public void Disable()
        {
            Loader.SongsLoadedEvent -= OnSongsLoaded;
        }


        private void OnSongsLoaded(Loader loader, Dictionary<string, CustomPreviewBeatmapLevel> levelDictionary)
        {
            if (!_firstRunDone)
            {
                _firstRunDone = true;
                return;
            }
            
            //Log.Info("SongCore finished loading, cleaning infos");
            _songDataCleaner.Run(levelDictionary.Values.ToList());
        }
    }
}