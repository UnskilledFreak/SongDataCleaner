using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SongCore;
using UnityEngine;

namespace SongDataCleaner
{
    public class SongDataCleaner : PersistentSingleton<SongDataCleaner>
    {
        private static IPA.Logging.Logger Log => Plugin.Log;
        
        internal static decimal CleanedSize;

        internal void Run(IEnumerable<CustomPreviewBeatmapLevel> levels)
        {
            StartCoroutine(InternalRun(levels));
        }

        private IEnumerator InternalRun(IEnumerable<CustomPreviewBeatmapLevel> levels)
        {
            // todo :: is waiting for 5 seconds this even needed?
            yield return new WaitForSeconds(5f);
            yield return new WaitUntil(() => Loader.AreSongsLoaded);
            
            var size = levels.Sum(CleanLevelData);

            CleanedSize = Math.Round((decimal) size / 1024, 2);

            PluginUI.instance.ShowProgress();
        }

        private long CleanLevelData(CustomPreviewBeatmapLevel level)
        {
            if (level == null)
            {
                Log.Warn("level is null");
                return 0;
            }

            var ignoreImages = false;

            //Log.Debug($"Cleaning {level.customLevelPath}");

            var whiteListedFiles = new List<string>
            {
                "info.dat",
                level.standardLevelInfoSaveData.coverImageFilename,
                level.standardLevelInfoSaveData.songFilename
            };

            
            // bug :: contributors not loaded
            var levelHash = level.levelID.Replace("custom_level_", "");
            var extraSongData = Collections.RetrieveExtraSongData(level.levelID);
            if (extraSongData == null)
            {
                Log.Warn($"could not get extra song data for level {level.levelID} | {level.customLevelPath}");
                ignoreImages = true;
            }
            else
            {
                whiteListedFiles.AddRange(
                    extraSongData.contributors.Select(contributor => contributor._iconPath)
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

            Log.Info($"level has possible unused files; ignoreImages = {ignoreImages} | {level.customLevelPath} ");

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

            Directory.GetDirectories(directory);

            list.AddRange(Directory.GetFiles(directory).ToList());

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