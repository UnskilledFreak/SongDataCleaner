using System.Collections.Generic;

namespace SongDataCleaner.Models
{
    // this is a lite version of the original MusicVideoPlayer plugin data model of video json
    public class VideoDatas
    {
        public int activeVideo { get; set; }
        public List<VideoData> videos { get; set; }

        public class VideoData
        {
            public string title { get; set; }
            public string author { get; set; }
            public string description { get; set; }
            public string duration { get; set; }
            public string URL { get; set; }
            public string thumbnailURL { get; set; }
            public bool loop { get; set; } = false;
            public int offset { get; set; } = 0; // ms
            public string videoPath { get; set; }
        }
    }
}