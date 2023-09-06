using BuildDataAccess.Entities;

namespace BuildDataAccess.DTOEntities
{
    public class UserDisplayLayerDTO : UserDisplayLayer
    {
        public short ResolutionId { get; set; }
        public short LicenseId { get; set; }
    }
}