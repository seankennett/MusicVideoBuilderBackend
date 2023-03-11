using Microsoft.Extensions.Options;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using System.Text.Json;
using Azure.Storage.Sas;
using Azure.Storage.Blobs.Models;
using SharedEntities;
using SharedEntities.Models;

namespace SpaWebApi.Services
{
    public class StorageService : IStorageService
    {
        private readonly Lazy<BlobServiceClient> _blobServiceClient;
        private readonly Lazy<QueueClient> _queueClientImageProcess;
        public StorageService(IOptions<Connections> connections)
        {
            _blobServiceClient = new Lazy<BlobServiceClient>(() => new BlobServiceClient(connections.Value.PrivateStorageConnectionString));
            _queueClientImageProcess = new Lazy<QueueClient>(() => new QueueClient(connections.Value.PrivateStorageConnectionString, SharedConstants.UploadLayerQueue, new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            }));
        }

        public async Task<bool> ValidateAudioBlob(string containerName, string blobName)
        {
            var containerClient = _blobServiceClient.Value.GetBlobContainerClient(containerName);
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
            var containerClient = _blobServiceClient.Value.GetBlobContainerClient(layerId.ToString());
            await containerClient.CreateAsync();

            // create write policy
            BlobSignedIdentifier identifier = new BlobSignedIdentifier
            {
                Id = layerId.ToString(),
                AccessPolicy = new BlobAccessPolicy
                {
                    PolicyExpiresOn = DateTime.UtcNow.AddHours(2),
                    Permissions = "w"
                }
            };
            await containerClient.SetAccessPolicyAsync(PublicAccessType.None, new List<BlobSignedIdentifier> { identifier });

            BlobSasBuilder builder = new BlobSasBuilder { Identifier = layerId.ToString() };
            return containerClient.GenerateSasUri(builder);
        }

        public async Task<Uri> CreateBlobAsync(string containerName, string blobName, bool isNewContainer, TimeSpan sasTokenLength)
        {
            var containerClient = _blobServiceClient.Value.GetBlobContainerClient(containerName);
            if (isNewContainer)
            {
                await containerClient.CreateIfNotExistsAsync();
            }

            var blobClient = containerClient.GetBlobClient(blobName);
            // on payment decline this may already exist
            await blobClient.DeleteIfExistsAsync();

            return blobClient.GenerateSasUri(BlobSasPermissions.Write, DateTime.UtcNow.Add(sasTokenLength));
        }

        public async Task RemoveContainerPolicySendToQueueAsync(LayerUploadMessage layerUpload)
        {
            var containerClient = _blobServiceClient.Value.GetBlobContainerClient(layerUpload.LayerId.ToString());
            if (await containerClient.ExistsAsync())
            {
                await containerClient.SetAccessPolicyAsync(PublicAccessType.None, null);
            }

            await _queueClientImageProcess.Value.SendMessageAsync(JsonSerializer.Serialize(layerUpload));
        }       

        public async Task<Uri?> GetSASLink(string userContainerName, string blobPrefix, string excludePrefix, DateTimeOffset tokenLength)
        {
            var containerClient = _blobServiceClient.Value.GetBlobContainerClient(userContainerName);
            var blobs = containerClient.GetBlobsAsync(prefix: blobPrefix);
            await foreach (var blob in blobs)
            {
                if (!blob.Name.Contains(excludePrefix))
                {
                    var blobClient = containerClient.GetBlobClient(blob.Name);
                    return blobClient.GenerateSasUri(BlobSasPermissions.Read, tokenLength);
                }
            }

            return null;
        }
    }
}
