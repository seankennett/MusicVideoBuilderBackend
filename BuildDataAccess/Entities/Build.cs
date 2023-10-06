using BuildEntities;
using SharedEntities.Models;
using VideoEntities.Entities;

namespace BuildDataAccess.Entities
{
    public class Build
    {
        public Guid BuildId { get; set; }
        public int? VideoId { get; set; }
        public string VideoName { get; set; }
        public BuildStatus BuildStatus { get; set; }
        public Resolution Resolution { get; set; }
        public License License { get; set; }
        public string? PaymentIntentId { get; set; }
        public bool HasAudio { get; set; }
        public DateTimeOffset DateUpdated { get; set; }
        public Formats Format { get; set; }
    }
}