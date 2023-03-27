using BuildEntities;

namespace BuildDataAccess.Entities
{
    public class UserLayer
    {
        public int UserLayerId { get; set; }
        public Resolution Resolution { get; set; }
        public License License { get; set; }
        public Guid LayerId { get; set; }
        public string LayerName { get; set; }
    }
}