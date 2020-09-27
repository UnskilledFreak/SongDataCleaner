using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SongDataCleaner.Models;

namespace SongDataCleaner
{
    public partial class SongDataCleaner
    {
        private List<string> HandleMusicVideoData(CustomPreviewBeatmapLevel level, string videoDataFile)
        {
            var returnList = new List<string>();
            var fileData = File.ReadAllText(videoDataFile);

            if (fileData.Substring(0, 1) == "{")
            {
                var video = JsonConvert.DeserializeObject<VideoDatas.VideoData>(fileData);
                if (!string.IsNullOrWhiteSpace(video.videoPath))
                {
                    AddIfExists(returnList, level, video.videoPath);
                }
            }
            else
            {
                var videoData = JsonConvert.DeserializeObject<VideoDatas>(fileData);
                if (videoData?.videos == null || !videoData.videos.Any())
                {
                    return returnList;
                }
                
                foreach (var dataVideo in videoData.videos)
                {
                    AddIfExists(returnList, level, dataVideo.videoPath);
                }
            }

            return returnList;
        }
    }
}