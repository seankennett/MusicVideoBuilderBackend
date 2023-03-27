using LayerDataAccess.Entities;

namespace SpaWebApi.Services
{
    public interface IStorageService
    {
        Task<bool> ValidateAudioBlob(string containerName, string blobName);
        Task<Uri> CreateUploadContainerAsync(Guid layerId);
        Task RemoveContainerPolicySendToUploadLayerQueueAsync(LayerUploadMessage layerUpload);
        Task<Uri> CreateBlobAsync(string containerName, string blobName, bool isNewContainer, DateTimeOffset tokenLength);
        Task<Uri?> GetSASLink(string userContainerName, string blobPrefix, string excludePrefix, DateTimeOffset tokenLength);
        Task SendToBuildInstructorQueueAsync(UserBuild userBuild);
    }
}