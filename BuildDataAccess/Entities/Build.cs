using BuildEntities;
using SharedEntities.Models;

namespace BuildDataAccess.Entities
{
    public class Build
    {
        public Guid BuildId { get; set; }
        public int VideoId { get; set; }
        public BuildStatus BuildStatus { get; set; }
        public Resolution Resolution { get; set; }
        public License License { get; set; }
        public string? PaymentIntentId { get; set; }
        public bool HasAudio { get; set; }
        public DateTimeOffset DateUpdated { get; set; }
    }
}