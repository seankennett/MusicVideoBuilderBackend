using LayerEntities;

namespace LayerDataAccess.DTOEntities
{
    public class LayerFinderDTO : Layer
    {
        public byte LayerTypeId { get; set; }
        public long UserCount { get; set; }
    }
}