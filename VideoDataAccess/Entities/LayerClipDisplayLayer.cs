using System.ComponentModel.DataAnnotations;

namespace VideoDataAccess.Entities
{
    public class LayerClipDisplayLayer
    {
        public Guid LayerId { get; set; }

        [MinLength(6)]
        [MaxLength(6)]
        [RegularExpression(@"\A\b[0-9a-fA-F]+\b\Z")]
        public string Colour { get; set; }
        
        [MinLength(6)]
        [MaxLength(6)]
        [RegularExpression(@"\A\b[0-9a-fA-F]+\b\Z")]
        public string? EndColour { get; set; }
    }
}