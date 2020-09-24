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
            SongDataCleaner.ResetCleanData();
            
            if (!Loader.AreSongsLoading)
            {
                Loader.Instance.RefreshSongs();
            }
            
            yield return new WaitUntil(() => Loader.AreSongsLoaded);
            yield return new WaitUntil(() => !SongDataCleaner.IsInCleanerRun);

            if (SongDataCleaner.CleanedSize == 0)
            {
                Plugin.Log.Info("nothing cleaned, disabled display of progress bar");
                yield break;
            }
            
            // some small time buffer to prevent text confusion with song data load result
            yield return new WaitForSeconds(3f);
            ShowProgress();
        }

        public void ShowProgress()
        {
            _progressBar.enabled = true;
            _progressBar.ShowMessage($"{SongDataCleaner.CleanedSize} {SongDataCleaner.CleanedUnit} cleaned", MessageTime);
        }
    }
}