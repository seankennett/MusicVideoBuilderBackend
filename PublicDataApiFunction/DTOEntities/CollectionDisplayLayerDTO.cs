using CollectionEntities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicDataApiFunction.DTOEntities
{
    public class CollectionDisplayLayerDTO : CollectionDisplayLayer
    {
        public Guid CollectionId { get; set; }
    }
}
