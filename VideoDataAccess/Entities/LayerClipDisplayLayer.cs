using System.ComponentModel.DataAnnotations;

namespace VideoDataAccess.Entities
{
    public class LayerClipDisplayLayer
    {
        public Guid LayerId { get; set; }

        [MinLength(6)]
        [MaxLength(6)]
        public string ColourOverride { get; set; }
    }
}