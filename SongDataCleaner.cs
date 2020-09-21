using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SongCore;
using SongCore.Data;
using UnityEngine;

namespace SongDataCleaner
{
    public class SongDataCleaner : PersistentSingleton<SongDataCleaner>
    {
        private static IPA.Logging.Logger Log => Plugin.Log;

        internal static string CleanedSize;

        internal void Run(IEnumerable<CustomPreviewBeatmapLevel> levels, bool showProgressBar = true)
        {
            StartCoroutine(InternalRun(levels, showProgressBar));
        }

        private IEnumerator InternalRun(IEnumerable<CustomPreviewBeatmapLevel> levels, bool showProgressBar = true)
        {
            yield return new WaitUntil(() => Loader.AreSongsLoaded);

            try
            {
                var size = levels.Sum(CleanLevelData);
                Log.Info($"cleaned {size} bytes");

                CleanedSize = GetHumanReadableFileSize(size);

                if (showProgressBar)
                {
                    PluginUI.instance.ShowProgress();
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private string GetHumanReadableFileSize(long bytes)
        {
            if (bytes == 0)
            {
                return "0 B";
            }
            
            // we should not exceed more than 1023 MB, wont we?
            var extensions = new [] {"B", "KB", "MB"};

            var factor = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));

            if (factor > 2)
            {
                factor = 2;
            }

            var num = Math.Round(bytes / Math.Pow(1024, factor), 2);

            return Math.Sign(bytes * num) + extensions[factor];
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

            var whiteListedFiles = new List<string>
            {
                "info.dat",
                level.standardLevelInfoSaveData.coverImageFilename,
                level.standardLevelInfoSaveData.songFilename
            };


            // bug :: contributors not loaded
            var levelHash = level.levelID.Replace("custom_level_", "");
            var extraSongData = Collections.RetrieveExtraSongData(levelHash);
            if (extraSongData == null)
            {
                Log.Warn($"could not get extra song data for level {levelHash} - setting ignore image flag | {shortPath}");
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

            Log.Info($"level has possible unused files; ignoreImages = {ignoreImages} | {shortPath} ");

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
                    Log.Debug($"extension is {extension}");
                    if (extension == "png" || extension == "jpg" || extension == "jpeg")
                    {
                        Log.Warn("----> file is an image but image skip is set");
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