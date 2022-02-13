using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayBattleRoyal
{
    public partial class MapDetailsModel
    {
        [JsonProperty("_version")]
        public string Version { get; set; }

        [JsonProperty("_BPMChanges")]
        public List<object> BpmChanges { get; set; }

        [JsonProperty("_events")]
        public List<Event> Events { get; set; }

        [JsonProperty("_notes")]
        public List<Note> Notes { get; set; }

        [JsonProperty("_obstacles")]
        public List<object> Obstacles { get; set; }

        [JsonProperty("_bookmarks")]
        public List<object> Bookmarks { get; set; }
    }

    public partial class Event
    {
        [JsonProperty("_time")]
        public double Time { get; set; }

        [JsonProperty("_type")]
        public long Type { get; set; }

        [JsonProperty("_value")]
        public long Value { get; set; }
    }

    public partial class Note
    {
        [JsonProperty("_time")]
        public double Time { get; set; }

        [JsonProperty("_lineIndex")]
        public long LineIndex { get; set; }

        [JsonProperty("_lineLayer")]
        public long LineLayer { get; set; }

        [JsonProperty("_type")]
        public long Type { get; set; }

        [JsonProperty("_cutDirection")]
        public long CutDirection { get; set; }
    }
}
