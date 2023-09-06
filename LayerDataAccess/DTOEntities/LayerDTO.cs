using LayerEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LayerDataAccess.DTOEntities
{
    public class LayerDTO: Layer
    {
        public Guid DisplayLayerId { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
