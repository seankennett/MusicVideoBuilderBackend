using LayerEntities;

namespace VideoDataAccess.DTOEntities
{
    public class ClipLayerDTO : Layer
    {
        public int ClipId { get; set; }
        public byte Order { get; set; }
        public short UserLayerStatusId { get; set; }
    }
}
