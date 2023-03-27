using VideoDataAccess.Entities;

namespace VideoDataAccess.DTOEntities
{
    public class VideoDTO : Video
    {
        public byte FormatId { get; set; }
    }
}