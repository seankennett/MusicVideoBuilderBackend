namespace BuildDataAccess.Extensions
{
    public static class GuidHelper
    {
        public static string GetUserContainerName(Guid userObjectId)
        {
            return $"user-{userObjectId}";
        }
        public static string GetTempBlobPrefix(Guid buildId)
        {
            return $"{buildId}/{BuildDataAccessConstants.TempBlobPrefix}";
        }
        public static string GetAudioBlobName(Guid buildId)
        {
            return $"{GetTempBlobPrefix(buildId)}/{BuildDataAccessConstants.AudioFileName}";
        }
    }
}
