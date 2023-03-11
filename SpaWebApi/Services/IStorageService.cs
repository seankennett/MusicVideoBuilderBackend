﻿using SharedEntities.Models;

namespace SpaWebApi.Services
{
    public interface IStorageService
    {
        Task<bool> ValidateAudioBlob(string containerName, string blobName);
        Task<Uri> CreateUploadContainerAsync(Guid layerId);
        Task RemoveContainerPolicySendToQueueAsync(LayerUploadMessage layerUpload);
        Task<Uri> CreateBlobAsync(string containerName, string blobName, bool isNewContainer, TimeSpan sasTokenLength);
        Task<Uri?> GetSASLink(string userContainerName, string blobPrefix, string excludePrefix, DateTimeOffset tokenLength);
    }
}