using LayerDataAccess.Entities;

namespace LayerDataAccess.DTOEntities
{
    public class LayerTagDTO : Tag
    {
        public Guid LayerId { get; set; }
    }
}
