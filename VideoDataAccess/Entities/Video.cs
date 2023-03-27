using System.ComponentModel.DataAnnotations;
using VideoDataAccess.Attributes;

namespace VideoDataAccess.Entities
{
    public class Video
    {
        [Required]
        [MaxLength(50)]
        [RegularExpression(@"^[a-zA-Z0-9]*$")]
        public string VideoName { get; set; }

        public int VideoId { get; set; }

        public Formats Format { get; set; }

        [Required]
        [Range(90, byte.MaxValue)]
        public byte BPM { get; set; }

        [Required]
        [MinLength(1)]
        [MaxLength(short.MaxValue)]
        [ClipLength("BPM", 15)]
        public IEnumerable<Clip> Clips { get; set; }

        [Range(0, int.MaxValue)]
        public int? VideoDelayMilliseconds { get; set; }
    }
}