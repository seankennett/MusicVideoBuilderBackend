using BuildDataAccess.Entities;

namespace BuildDataAccess.DTOEntities
{
    public class BuildDTO : Build
    {
        public short BuildStatusId { get; set; }
        public short ResolutionId { get; set; }
        public short LicenseId { get; set; }
        public short FormatId { get; set; }
        public Guid? UserObjectId { get; set; }
    }
}
