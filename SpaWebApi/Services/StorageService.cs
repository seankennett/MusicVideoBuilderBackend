using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using System.Text.Json;
using Azure.Storage.Sas;
using Azure.Storage.Blobs.Models;
using SharedEntities;
using SharedEntities.Models;
using System.Reflection.Metadata;
using static System.Reflection.Metadata.BlobBuilder;

namespace SpaWebApi.Services
{
    public class StorageService : IStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly QueueClient _queueClientUploadLayer;
        private readonly QueueClient _queueClientBuildInstructor;
        public StorageService(BlobServiceClient blobServiceClient, QueueServiceClient queueServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            _queueClientUploadLayer = queueServiceClient.GetQueueClient(SharedConstants.UploadLayerQueue);
            _queueClientBuildInstructor = queueServiceClient.GetQueueClient(SharedConstants.BuildInstructorQueue);
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

        public async Task<Uri> CreateUploadContainerAsync(Guid layerId)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(layerId.ToString());
            await containerClient.CreateAsync();

            // not supported with identities https://learn.microsoft.com/en-us/rest/api/storageservices/set-container-acl
            //BlobSignedIdentifier identifier = new BlobSignedIdentifier
            //{
            //    Id = layerId.ToString(),
            //    AccessPolicy = new BlobAccessPolicy
            //    {
            //        PolicyExpiresOn = DateTime.UtcNow.AddHours(2),
            //        Permissions = "w"
            //    }
            //};
            //await containerClient.SetAccessPolicyAsync(PublicAccessType.None, new List<BlobSignedIdentifier> { identifier });

            //BlobSasBuilder builder = new BlobSasBuilder { Identifier = layerId.ToString() };

            var url = await GenerateUserDelegateSas(BlobAccountSasPermissions.Write, containerClient, null, DateTimeOffset.UtcNow.AddMinutes(10));

            var test = new BlobContainerClient(url);

            await test.UploadBlobAsync("test", new BinaryData("test"));

            return url;
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

        public async Task RemoveContainerPolicySendToUploadLayerQueueAsync(LayerUploadMessage layerUpload)
        {
            // with identities this is not supported https://learn.microsoft.com/en-us/rest/api/storageservices/set-container-acl
            //var containerClient = _blobServiceClient.GetBlobContainerClient(layerUpload.LayerId.ToString());
            //if (await containerClient.ExistsAsync())
            //{
            //    await containerClient.SetAccessPolicyAsync(PublicAccessType.None, null);
            //}

            await _queueClientUploadLayer.SendMessageAsync(JsonSerializer.Serialize(layerUpload));
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
            var userDelegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow, expiresOn);
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerClient.Name,                
                Resource = "c",
                StartsOn = DateTimeOffset.UtcNow,
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
