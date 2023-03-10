using SharedEntities.Attributes;
using System.ComponentModel.DataAnnotations;

namespace SharedEntities.Models
{
    public class Clip
    {
        [Required]
        [MaxLength(50)]
        [RegularExpression(@"^[a-zA-Z0-9]*$")]
        public string ClipName { get; set; }
        public int ClipId { get; set; }

        [MaxLength(SharedConstants.MaximumLayerPerClip)]
        [UniqueList("LayerId")]
        public IEnumerable<Layer>? Layers { get; set; }

        [MinLength(6)]
        [MaxLength(6)]
        public string? BackgroundColour { get; set; }

        [Range(1, SharedConstants.BeatsPerLayer)]
        public byte BeatLength { get; set; }

        [Range(1, SharedConstants.BeatsPerLayer)]
        public byte StartingBeat { get; set; }
    }
}