using SharedEntities.Models;
using VideoEntities.Entities;

namespace BuildDataAccess.Entities
{
    public class BuildAsset
    {
        public int VideoId { get; set; }
        public string VideoName { get; set; }
        public Formats Format { get; set; }
        public DateTimeOffset DateCreated { get; set; }
        public Uri? DownloadLink { get; set; }
        public BuildStatus BuildStatus { get; set; }
    }
}