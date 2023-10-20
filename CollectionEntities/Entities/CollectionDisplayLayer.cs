namespace CollectionEntities.Entities
{
    public class CollectionDisplayLayer
    {
        public Guid DisplayLayerId { get; set; }
        public IEnumerable<LayerCollectionDisplayLayer> LayerCollectionDisplayLayers { get; set; }
    }
}