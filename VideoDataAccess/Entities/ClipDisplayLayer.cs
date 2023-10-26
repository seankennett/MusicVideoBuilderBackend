using System.ComponentModel.DataAnnotations;

namespace VideoDataAccess.Entities
{
    public class ClipDisplayLayer
    {
        public Guid DisplayLayerId { get; set; }
        public IEnumerable<LayerClipDisplayLayer> LayerClipDisplayLayers { get;set;}
        public bool Reverse { get; set; }
        public bool FlipHorizontal { get; set; }
        public bool FlipVertical { get; set; }
        public FadeTypes? FadeType { get; set; }
        
        [MinLength(6)]
        [MaxLength(6)]
        [RegularExpression(@"\A\b[0-9a-fA-F]+\b\Z")]
        public string? Colour { get; set; }
    }
}