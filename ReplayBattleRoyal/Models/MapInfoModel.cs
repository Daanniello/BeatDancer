using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayBattleRoyal.Models
{
    class MapInfoModel
    {

        [JsonProperty("_version")]
        public string Version { get; set; }

        [JsonProperty("_songName")]
        public string SongName { get; set; }

        [JsonProperty("_songSubName")]
        public string SongSubName { get; set; }

        [JsonProperty("_songAuthorName")]
        public string SongAuthorName { get; set; }

        [JsonProperty("_levelAuthorName")]
        public string LevelAuthorName { get; set; }

        [JsonProperty("_beatsPerMinute")]
        public long BeatsPerMinute { get; set; }

        [JsonProperty("_shuffle")]
        public long Shuffle { get; set; }

        [JsonProperty("_shufflePeriod")]
        public double ShufflePeriod { get; set; }

        [JsonProperty("_previewStartTime")]
        public long PreviewStartTime { get; set; }

        [JsonProperty("_previewDuration")]
        public long PreviewDuration { get; set; }

        [JsonProperty("_songFilename")]
        public string SongFilename { get; set; }

        [JsonProperty("_coverImageFilename")]
        public string CoverImageFilename { get; set; }

        [JsonProperty("_environmentName")]
        public string EnvironmentName { get; set; }

        [JsonProperty("_allDirectionsEnvironmentName")]
        public string AllDirectionsEnvironmentName { get; set; }

        [JsonProperty("_songTimeOffset")]
        public long SongTimeOffset { get; set; }

        [JsonProperty("_customData")]
        public MapInfoModelCustomData CustomData { get; set; }

        [JsonProperty("_difficultyBeatmapSets")]
        public List<DifficultyBeatmapSet> DifficultyBeatmapSets { get; set; }
    }

    public partial class MapInfoModelCustomData
    {
        [JsonProperty("_contributors")]
        public List<Contributor> Contributors { get; set; }

        [JsonProperty("_editors")]
        public Editors Editors { get; set; }
    }

    public partial class Contributor
    {
        [JsonProperty("_role")]
        public string Role { get; set; }

        [JsonProperty("_name")]
        public string Name { get; set; }

        [JsonProperty("_iconPath")]
        public string IconPath { get; set; }
    }

    public partial class Editors
    {
        [JsonProperty("_lastEditedBy")]
        public string LastEditedBy { get; set; }

        [JsonProperty("ChroMapper")]
        public ChroMapper ChroMapper { get; set; }
    }

    public partial class ChroMapper
    {
        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public partial class DifficultyBeatmapSet
    {
        [JsonProperty("_beatmapCharacteristicName")]
        public string BeatmapCharacteristicName { get; set; }

        [JsonProperty("_difficultyBeatmaps")]
        public List<DifficultyBeatmap> DifficultyBeatmaps { get; set; }
    }

    public partial class DifficultyBeatmap
    {
        [JsonProperty("_difficulty")]
        public string Difficulty { get; set; }

        [JsonProperty("_difficultyRank")]
        public long DifficultyRank { get; set; }

        [JsonProperty("_beatmapFilename")]
        public string BeatmapFilename { get; set; }

        [JsonProperty("_noteJumpMovementSpeed")]
        public long NoteJumpMovementSpeed { get; set; }

        [JsonProperty("_noteJumpStartBeatOffset")]
        public double NoteJumpStartBeatOffset { get; set; }

        [JsonProperty("_customData")]
        public DifficultyBeatmapCustomData CustomData { get; set; }
    }

    public partial class DifficultyBeatmapCustomData
    {
        [JsonProperty("_difficultyLabel")]
        public string DifficultyLabel { get; set; }
    }
}
