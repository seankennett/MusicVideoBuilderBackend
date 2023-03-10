using SharedEntities.Models;

namespace SpaWebApi.Services
{
    public interface IStorageService
    {
        Task<bool> ValidateAudioBlob(string containerName, string blobName);
        Task<Uri> CreateUploadContainerAsync(Guid layerId);
        Task<Uri> CreateContainerAsync(string containerName, bool isNew, TimeSpan sasTokenLength);
        Task RemoveContainerPolicySendToQueueAsync(LayerUploadMessage layerUpload);
        Task<Uri> CreateBlobAsync(string containerName, string blobName, bool isNewContainer, TimeSpan sasTokenLength);
        Task UploadTextFile(string containerName, string blobPrefix, string fileName, string contents, bool createContainer);
        Task<Uri?> GetSASLink(string userContainerName, string blobPrefix, string excludePrefix, DateTimeOffset tokenLength);
    }
}