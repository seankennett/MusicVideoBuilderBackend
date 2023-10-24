namespace VideoDataAccess.Entities
{
    public class ClipDisplayLayer
    {
        public Guid DisplayLayerId { get; set; }
        public IEnumerable<LayerClipDisplayLayer> LayerClipDisplayLayers { get;set;}
        public bool Reverse { get; set; }
        public bool FlipHorizontal { get; set; }
        public bool FlipVertical { get; set; }
    }
}