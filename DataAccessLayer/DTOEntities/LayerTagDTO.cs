using SharedEntities.Models;

namespace DataAccessLayer.DTOEntities
{
    public class LayerTagDTO : Tag
    {
        public Guid LayerId { get; set; }
    }
}
