namespace CollectionEntities.Entities
{
    public class DisplayLayer
    {
        public Guid DisplayLayerId { get; set; }
        public bool IsCollectionDefault { get; set; }
        public Direction Direction { get; set; }
        public short NumberOfSides { get; set; }
        public IEnumerable<Layer> Layers { get; set; }
        public Guid? LinkedPreviousDisplayLayerId { get; set; }
    }
}