using SharedEntities.Models;

namespace DataAccessLayer.DTOEntities
{
    public class VideoClipDTO : Clip
    {
        public int VideoId { get; set; }
        public short Order { get; set; }
    }
}
