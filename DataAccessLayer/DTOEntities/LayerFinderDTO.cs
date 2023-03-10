using SharedEntities.Models;

namespace DataAccessLayer.DTOEntities
{
    public class LayerFinderDTO : Layer
    {
        public byte LayerTypeId { get; set; }
        public long UserCount { get; set; }
    }
}