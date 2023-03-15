﻿namespace SharedEntities.Extensions
{
    public static class GuidHelper
    {
        public static string GetUserContainerName(Guid userObjectId)
        {
            return $"user-{userObjectId}";
        }
        public static string GetTempBlobPrefix(Guid buildId)
        {
            return $"{buildId}/{SharedConstants.TempBlobPrefix}";
        }
        public static string GetAudioBlobName(Guid buildId)
        {
            return $"{GetTempBlobPrefix(buildId)}/{SharedConstants.AudioFileName}";
        }
    }
}
