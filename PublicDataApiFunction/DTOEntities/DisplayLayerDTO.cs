using CollectionEntities.Entities;
using System;

namespace PublicDataApiFunction.DTOEntities
{
    public class DisplayLayerDTO : DisplayLayer
    {
        public short DirectionId { get; set; }
        public Guid CollectionId { get; set; }
    }
}