using BuildEntities;

namespace BuildDataAccess.Entities
{
    public class UserCollection
    {
        public int UserCollectionId { get; set; }
        public Resolution Resolution { get; set; }
        public License License { get; set; }
        public Guid CollectionId { get; set; }
    }
}