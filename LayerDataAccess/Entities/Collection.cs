namespace LayerDataAccess.Entities
{
    public class Collection
    {
        public Guid CollectionId { get; set; }
        public string CollectionName { get; set; }
        public CollectionType CollectionType { get; set; }        
        public IEnumerable<DisplayLayer> DisplayLayers { get; set; }
        public int UserCount { get; set; }
    }
}