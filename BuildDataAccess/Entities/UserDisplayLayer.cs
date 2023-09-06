using BuildEntities;

namespace BuildDataAccess.Entities
{
    public class UserDisplayLayer
    {
        public int UserDisplayLayerId { get; set; }
        public Resolution Resolution { get; set; }
        public License License { get; set; }
        public Guid DisplayLayerId { get; set; }
    }
}