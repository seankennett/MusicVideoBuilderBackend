using LayerDataAccess.Entities;

namespace LayerDataAccess.DTOEntities
{
    public class DisplayLayerDTO : DisplayLayer
    {
        public short DirectionId { get; set; }
        public Guid CollectionId { get; set; }
    }
}