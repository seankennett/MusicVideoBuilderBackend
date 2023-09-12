namespace SpaWebApi.Services
{
    public interface IStorageService
    {
        Task<bool> ValidateAudioBlob(string containerName, string blobName);
        Task<Uri> CreateUploadContainerAsync(Guid layerId);
        Task<Uri> CreateBlobAsync(string containerName, string blobName, bool isNewContainer, DateTimeOffset tokenLength);
        Task<Uri?> GetSASLink(string userContainerName, string blobPrefix, string excludePrefix, DateTimeOffset tokenLength);
        Task SendToBuildInstructorQueueAsync(UserBuild userBuild);
    }
}