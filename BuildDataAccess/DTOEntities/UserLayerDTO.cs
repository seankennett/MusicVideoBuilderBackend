using BuildDataAccess.Entities;

namespace BuildDataAccess.DTOEntities
{
    public class UserLayerDTO : UserLayer
    {
        public short ResolutionId { get; set; }
        public short LicenseId { get; set; }
    }
}