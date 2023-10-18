using VideoDataAccess.Entities;

namespace VideoDataAccess.DTOEntities
{
    public class VideoClipDTO : VideoClip
    {
        public int VideoId { get; set; }
        public short Order { get; set; }
    }
}
