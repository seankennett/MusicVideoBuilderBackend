using CollectionEntities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicDataApiFunction.DTOEntities
{
    public class LayerDTO : Layer
    {
        public Guid DisplayLayerId { get; set; }
        public byte Order { get; set; }
    }
}
