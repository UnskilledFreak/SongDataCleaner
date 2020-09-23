using System.Collections;
using BeatSaberMarkupLanguage.MenuButtons;
using SongCore;
using UnityEngine;

namespace SongDataCleaner
{
    public class PluginUI : PersistentSingleton<PluginUI>
    {
        private static ProgressBar _progressBar;
        private MenuButton _menuButton;
        
        private const int MessageTime = 5;

        internal void Setup()
        {
            _menuButton = new MenuButton("Clean Song Data", "Forces refreshing of all Songs & Playlists and cleans song data folders", RefreshButtonPressed);
            MenuButtons.instance.RegisterButton(_menuButton);
            StartCoroutine(InternalSetup());
        }

        private void RefreshButtonPressed()
        {
            StartCoroutine(Refresh());
        }

        private IEnumerator InternalSetup()
        {
            yield return new WaitUntil(() => Loader.Instance != null);
            
            _progressBar = ProgressBar.Create();
        }

        private IEnumerator Refresh()
        {
            if (!Loader.AreSongsLoading)
            {
                Loader.Instance.RefreshSongs();
            }
            
            yield return new WaitUntil(() => Loader.AreSongsLoaded);

            if (SongDataCleaner.CleanedSize == 0)
            {
                Plugin.Log.Info("nothing cleaned, disabled display of progress bar");
                yield break;
            }
            
            // wait for SongCores message to prevent mixing with PlaylistLoaderLite and other mods 
            yield return new WaitForSeconds(5f);
            ShowProgress();
        }

        public void ShowProgress()
        {
            _progressBar.enabled = true;
            _progressBar.ShowMessage($"{SongDataCleaner.CleanedSize} {SongDataCleaner.CleanedUnit} cleaned", MessageTime);
        }
    }
}