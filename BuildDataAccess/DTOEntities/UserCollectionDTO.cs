using BuildDataAccess.Entities;

namespace BuildDataAccess.DTOEntities
{
    public class UserCollectionDTO : UserCollection
    {
        public short ResolutionId { get; set; }
        public short LicenseId { get; set; }
    }
}