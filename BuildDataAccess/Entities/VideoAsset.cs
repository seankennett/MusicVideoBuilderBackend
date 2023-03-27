using SharedEntities.Models;

namespace BuildDataAccess.Entities
{
    public class VideoAsset
    {
        public int VideoId { get; set; }
        public DateTimeOffset DateCreated { get; set; }
        public Uri? DownloadLink { get; set; }
        public BuildStatus BuildStatus { get; set; }
    }
}