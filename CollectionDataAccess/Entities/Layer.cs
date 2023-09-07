namespace LayerEntities
{
    public class Layer
    {
        public Guid LayerId { get; set; }
        public bool IsOverlay { get; set; }
        public string DefaultColour { get; set; }
    }
}