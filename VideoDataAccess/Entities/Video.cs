using System.ComponentModel.DataAnnotations;
using VideoDataAccess.Attributes;
using VideoEntities.Entities;

namespace VideoDataAccess.Entities
{
    public class Video
    {
        [Required]
        [MaxLength(50)]
        [RegularExpression(@"^[a-zA-Z0-9_-]*$")]
        public string VideoName { get; set; }

        public int VideoId { get; set; }

        public Formats Format { get; set; }

        [Required]
        [Range(90, byte.MaxValue)]
        public byte BPM { get; set; }

        [Required]
        [MinLength(1)]
        [MaxLength(short.MaxValue)]
        public IEnumerable<VideoClip> VideoClips { get; set; }

        [Range(0, int.MaxValue)]
        public int? VideoDelayMilliseconds { get; set; }
    }
}