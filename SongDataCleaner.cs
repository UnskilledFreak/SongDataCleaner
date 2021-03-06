using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SongCore;
using UnityEngine;

namespace SongDataCleaner
{
    public partial class SongDataCleaner : PersistentSingleton<SongDataCleaner>
    {
        private static IPA.Logging.Logger Log => Plugin.Log;

        internal static double CleanedSize { get; set; }
        internal static string CleanedUnit { get; set; }

        internal static bool IsInCleanerRun { get; set; }

        internal void Run(List<CustomPreviewBeatmapLevel> levels, bool showProgressBar = true)
        {
            ResetCleanData();
            StartCoroutine(InternalRun(levels, showProgressBar));
        }

        internal static void ResetCleanData()
        {
            //Log.Debug("resetting clean data...");
            CleanedSize = 0;
            CleanedUnit = "B";
        }

        private IEnumerator InternalRun(List<CustomPreviewBeatmapLevel> levels, bool showProgressBar = true)
        {
            Log.Debug($"called internal run for {levels.Count} levels");

            //yield return new WaitUntil(() => !Loader.AreSongsLoading);
            yield return new WaitUntil(() => Loader.AreSongsLoaded);

            ResetCleanData();

            if (IsInCleanerRun)
            {
                Log.Debug("InternalRun get triggered but inRun is set");
                yield break;
            }

            try
            {
                Log.Debug("locking cleaner");
                IsInCleanerRun = true;
                var size = levels.Sum(CleanLevelData);

                Log.Info($"cleaned {size} bytes");

                SetHumanReadableFileSize(size);

                if (showProgressBar)
                {
                    StartCoroutine(PluginUI.instance.ShowProgress());
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            finally
            {
                Log.Debug("unlocking cleaner");
                IsInCleanerRun = false;
            }
        }

        private void SetHumanReadableFileSize(long bytes)
        {
            if (bytes == 0)
            {
                CleanedSize = 0;
                CleanedUnit = "B";
                return;
            }

            // we should not exceed more than a few hundred MB, wont we?
            var extensions = new[] {"B", "KB", "MB"};

            var factor = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));

            if (factor > 2)
            {
                factor = 2;
            }

            CleanedSize = Math.Round(bytes / Math.Pow(1024, factor), 2);
            CleanedUnit = extensions[factor];
        }

        private void AddIfExists(List<string> whiteListedFiles, CustomPreviewBeatmapLevel level, string file, Func<CustomPreviewBeatmapLevel, string, List<string>> callbackOnFound = null)
        {
            // this check is necessary for performance on comparing file lists
            var path = Path.Combine(level.customLevelPath, file);
            if (!File.Exists(path))
            {
                return;
            }

            //Log.Debug($"found {file}");
            whiteListedFiles.Add(file);

            if (callbackOnFound != null)
            {
                whiteListedFiles.AddRange(callbackOnFound.Invoke(level, path));
            }
        }

        private long CleanLevelData(CustomPreviewBeatmapLevel level)
        {
            if (level == null)
            {
                Log.Warn("level is null");
                return 0;
            }

            var ignoreImages = false;
            var shortPath = level.customLevelPath.Substring(level.customLevelPath.LastIndexOf("\\") + 1);
            //Log.Debug($"Cleaning {shortPath}");

            var whiteListedFiles = new List<string>();

            // standard data
            AddIfExists(whiteListedFiles, level, "info.dat");
            AddIfExists(whiteListedFiles, level, "Info.dat");
            AddIfExists(whiteListedFiles, level, level.standardLevelInfoSaveData.coverImageFilename); // uhm, why should it not exist?
            AddIfExists(whiteListedFiles, level, level.standardLevelInfoSaveData.songFilename);
            // BeatSinger
            AddIfExists(whiteListedFiles, level, "lyrics.srt");
            AddIfExists(whiteListedFiles, level, "lyrics.json");
            // MusicVideoPlayer
            AddIfExists(whiteListedFiles, level, "video.json", HandleMusicVideoData);
            // GameSaber
            AddIfExists(whiteListedFiles, level, "GameParams.json");

            var levelHash = level.levelID.Replace("custom_level_", "");
            var extraSongData = Collections.RetrieveExtraSongData(levelHash);
            if (extraSongData == null)
            {
                Log.Debug($"could not get extra song data for level {levelHash} - setting ignore image flag | {shortPath}");
                ignoreImages = true;
            }
            else
            {
                whiteListedFiles.AddRange(
                    extraSongData.contributors.Where(contributor => !string.IsNullOrWhiteSpace(contributor._iconPath)).Select(contributor => contributor._iconPath)
                );
            }

            whiteListedFiles.AddRange(
                from beatmapSet in level.standardLevelInfoSaveData.difficultyBeatmapSets
                from setDifficultyBeatmap in beatmapSet.difficultyBeatmaps
                select setDifficultyBeatmap.beatmapFilename
            );

            var entries = GetCompleteFileList(level.customLevelPath);

            if (entries.Count == whiteListedFiles.Count)
            {
                // equal file count, no need for further processing
                //Log.Debug("--> equal file count, skipping");
                return 0;
            }

            //Log.Debug($"level has possible unused files; ignoreImages = {ignoreImages} | {shortPath} ");

            for (var i = 0; i < whiteListedFiles.Count; i++)
            {
                whiteListedFiles[i] = Path.Combine(level.customLevelPath, whiteListedFiles[i]);
            }

            return CleanByFileList(whiteListedFiles, entries, ignoreImages);
        }

        private List<string> GetCompleteFileList(string directory)
        {
            var list = new List<string>();

            foreach (var dir in Directory.GetDirectories(directory))
            {
                list.AddRange(GetCompleteFileList(dir));
            }

            list.AddRange(Directory.GetFiles(directory).ToList());
            list.Add(directory);

            return list;
        }

        private long CleanByFileList(IEnumerable<string> whitelist, IEnumerable<string> found, bool ignoreImages)
        {
            long totalSize = 0;
            var unusedFiles = found.Except(whitelist).ToList();

            if (unusedFiles.Count == 0)
            {
                return 0;
            }

            //Log.Info("deleting file(s):");
            foreach (var file in unusedFiles)
            {
                if (Directory.Exists(file))
                {
                    if (Directory.GetFiles(file).Length == 0 && Directory.GetDirectories(file).Length == 0)
                    {
                        Directory.Delete(file);
                    }

                    continue;
                }

                var info = new FileInfo(file);
                //Log.Info($"--> {file}");

                if (ignoreImages)
                {
                    var extension = info.Extension.Substring(1).ToLowerInvariant();
                    //Log.Debug($"extension is {extension}");
                    if (extension == "png" || extension == "jpg" || extension == "jpeg")
                    {
                        Log.Debug("----> file is an image but image skip is set");
                        continue;
                    }
                }

                totalSize += info.Length;
                File.Delete(file);
            }

            return totalSize;
        }
    }
}