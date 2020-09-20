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
        }

        private void RefreshButtonPressed()
        {
            StartCoroutine(Refresh());
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
            
            ShowProgress();
        }

        public void ShowProgress()
        {
            _progressBar = ProgressBar.Create();
            _progressBar.enabled = true;
            _progressBar.ShowMessage($"{SongDataCleaner.CleanedSize} KB cleaned", MessageTime);
        }
    }
}