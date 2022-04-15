using Newtonsoft.Json;
using System.Collections.Generic;

namespace ReplayBattleRoyal
{
    public partial class ReplayModel
    {
        [JsonProperty("info")]
        public Info Info { get; set; }

        [JsonProperty("frames")]
        public List<Frame> Frames { get; set; }

        [JsonProperty("scores")]
        public List<long> Scores { get; set; }

        [JsonProperty("combos")]
        public List<long> Combos { get; set; }

        [JsonProperty("noteTime")]
        public List<double> NoteTime { get; set; }

        [JsonProperty("noteInfos")]
        public List<string> NoteInfos { get; set; }

        [JsonProperty("dynamicHeight")]
        public List<object> DynamicHeight { get; set; }
    }

    public partial class Frame
    {
        [JsonProperty("h")]
        public H H { get; set; }

        [JsonProperty("l")]
        public H L { get; set; }

        [JsonProperty("r")]
        public H R { get; set; }

        [JsonProperty("i")]
        public long I { get; set; }

        [JsonProperty("a")]
        public double A { get; set; }
    }

    public partial class H
    {
        [JsonProperty("p")]
        public Room P { get; set; }

        [JsonProperty("r")]
        public Room R { get; set; }
    }

    public partial class Room
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("z")]
        public double Z { get; set; }

        [JsonProperty("w", NullValueHandling = NullValueHandling.Ignore)]
        public double? W { get; set; }
    }

    public partial class Info
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("difficulty")]
        public long Difficulty { get; set; }

        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("environment")]
        public string Environment { get; set; }

        [JsonProperty("modifiers")]
        public List<string> Modifiers { get; set; }

        [JsonProperty("noteJumpStartBeatOffset")]
        public double NoteJumpStartBeatOffset { get; set; }

        [JsonProperty("leftHanded")]
        public bool LeftHanded { get; set; }

        [JsonProperty("height")]
        public double Height { get; set; }

        [JsonProperty("rr")]
        public long Rr { get; set; }

        [JsonProperty("room")]
        public Room Room { get; set; }

        [JsonProperty("st")]
        public long St { get; set; }

        [JsonProperty("totalScore")]
        public long TotalScore { get; set; }

        [JsonProperty("midDeviation")]
        public long MidDeviation { get; set; }
    }
}
