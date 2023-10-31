using System.ComponentModel.DataAnnotations;
using VideoDataAccess.Attributes;

namespace VideoDataAccess.Entities
{
    public class Clip
    {
        [Required]
        [MaxLength(50)]
        [RegularExpression(@"^[a-zA-Z0-9_-]*$")]
        public string ClipName { get; set; }
        public int ClipId { get; set; }

        [MaxLength(VideoDataAccessConstants.MaximumDisplayLayerPerClip)]
        [UniqueList("DisplayLayerId")]
        public IEnumerable<ClipDisplayLayer>? ClipDisplayLayers { get; set; }

        [MinLength(6)]
        [MaxLength(6)]
        [RegularExpression(@"\A\b[0-9a-fA-F]+\b\Z")]
        public string? BackgroundColour { get; set; }

        [MinLength(6)]
        [MaxLength(6)]
        [RegularExpression(@"\A\b[0-9a-fA-F]+\b\Z")]
        public string? EndBackgroundColour { get; set; }

        [Range(1, VideoDataAccessConstants.BeatsPerDisplayLayer)]
        public byte BeatLength { get; set; }

        [Range(1, VideoDataAccessConstants.BeatsPerDisplayLayer)]
        public byte StartingBeat { get; set; }
    }
}