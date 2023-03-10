using SharedEntities.Models;

namespace DataAccessLayer.DTOEntities
{
    public class VideoDTO : Video
    {
        public byte FormatId { get; set; }
    }
}