using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoDataAccess.Entities;

namespace VideoDataAccess.DTOEntities
{
    public class ClipDisplayLayerDTO : ClipDisplayLayer
    {
        public int ClipDisplayLayerId { get; set; }
        public int ClipId { get; set; }
        public short Order { get; set; }
    }
}
