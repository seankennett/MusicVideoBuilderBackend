using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using System.Text.Json;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Azure.Storage.Blobs.Models;

namespace SpaWebApi.Services
{
    public class StorageService : IStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly QueueClient _queueClientBuildInstructor;
        private readonly IMemoryCache _memoryCache;
        private const string CacheKeyUserDelegationKey = "UserDelegationKey";

        public StorageService(BlobServiceClient blobServiceClient, QueueServiceClient queueServiceClient, IOptions<SpaWebApiConfig> config, IMemoryCache memoryCache)
        {
            _blobServiceClient = blobServiceClient;
            _queueClientBuildInstructor = queueServiceClient.GetQueueClient(config.Value.BuildInstructorQueueName);
            _memoryCache = memoryCache;
        }

        public async Task<bool> ValidateAudioBlob(string containerName, string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            if (!await blobClient.ExistsAsync())
            {
                return false;
            }

            var properties = await blobClient.GetPropertiesAsync();
            if (properties.Value.ContentLength > 40000000)
            {
                await blobClient.DeleteAsync();
                return false;
            }

            return true;
        }

        public async Task<Uri> CreateBlobAsync(string containerName, string blobName, bool isNewContainer, DateTimeOffset tokenLength)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            if (isNewContainer)
            {
                await containerClient.CreateIfNotExistsAsync();
            }

            var blobClient = containerClient.GetBlobClient(blobName);
            // on payment decline this may already exist
            await blobClient.DeleteIfExistsAsync();

            return await GenerateUserDelegateSas(BlobAccountSasPermissions.Write, containerClient, blobClient, tokenLength);
        }       

        public async Task<Uri?> GetSASLink(string userContainerName, string blobPrefix, string excludePrefix, DateTimeOffset tokenLength)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(userContainerName);
            var blobs = containerClient.GetBlobsAsync(prefix: blobPrefix);
            await foreach (var blob in blobs)
            {
                if (!blob.Name.Contains(excludePrefix))
                {
                    var blobClient = containerClient.GetBlobClient(blob.Name);
                    return await GenerateUserDelegateSas(BlobAccountSasPermissions.Read, containerClient, blobClient, tokenLength);
                }
            }

            return null;
        }

        public async Task SendToBuildInstructorQueueAsync(UserBuild userBuild)
        {
            await _queueClientBuildInstructor.SendMessageAsync(JsonSerializer.Serialize(userBuild));
        }

        private async Task<Uri> GenerateUserDelegateSas(BlobAccountSasPermissions permission, BlobContainerClient containerClient, BlobClient? blobClient, DateTimeOffset expiresOn)
        {
            var startsOn = DateTimeOffset.UtcNow;

            if (!_memoryCache.TryGetValue(CacheKeyUserDelegationKey, out UserDelegationKey userDelegationKey)) 
            {
                // max 7 day span for user key
                userDelegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(startsOn, startsOn.AddDays(7));
                // set cache earlier so this is never in threat of being stale whilst call happens
                _memoryCache.Set(CacheKeyUserDelegationKey, userDelegationKey, startsOn.AddDays(6));
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerClient.Name,                
                Resource = "c",
                StartsOn = startsOn,
                ExpiresOn = expiresOn,
            };
            var uri = containerClient.Uri;

            if (blobClient != null)
            {
                sasBuilder.BlobName = blobClient.Name;
                sasBuilder.Resource = "b";
                uri = blobClient.Uri;
            }

            sasBuilder.SetPermissions(permission);
            var blobUriBuilder = new BlobUriBuilder(uri)
            {
                Sas = sasBuilder.ToSasQueryParameters(userDelegationKey, _blobServiceClient.AccountName)
            };

            return blobUriBuilder.ToUri();
        }
    }
}
