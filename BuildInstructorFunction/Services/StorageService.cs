using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using SharedEntities.Extensions;
using SharedEntities;
using SharedEntities.Models;
using System;
using System.Threading.Tasks;
using System.Text.Json;

namespace BuildInstructorFunction.Services
{
    public class StorageService : IStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly QueueClient _queueClientFree;
        private readonly QueueClient _queueClientHd;

        public StorageService(BlobServiceClient blobServiceClient, QueueServiceClient queueServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            _queueClientFree = queueServiceClient.GetQueueClient(Resolution.Free.GetBlobPrefixByResolution() + SharedConstants.BuilderQueueSuffix);
            _queueClientHd = queueServiceClient.GetQueueClient(Resolution.Hd.GetBlobPrefixByResolution() + SharedConstants.BuilderQueueSuffix);
        }

        public Uri GetContainerSasUri(string containerName, TimeSpan sasTokenLength)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            return containerClient.GenerateSasUri(BlobContainerSasPermissions.Write, DateTime.UtcNow.Add(sasTokenLength));
        }

        public async Task SendFreeBuilderMessageAsync(BuilderMessage builderMessage)
        {
            await _queueClientFree.SendMessageAsync(JsonSerializer.Serialize(builderMessage));
        }

        public async Task SendHdBuilderMessageAsync(BuilderMessage builderMessage)
        {
            await _queueClientHd.SendMessageAsync(JsonSerializer.Serialize(builderMessage));
        }

        public async Task UploadTextFile(string containerName, string blobPrefix, string fileName, string contents, bool createContainer)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            if (createContainer)
            {
                await containerClient.CreateIfNotExistsAsync();
            }

            var blobClient = containerClient.GetBlobClient($"{blobPrefix}/{fileName}");
            await blobClient.UploadAsync(BinaryData.FromString(contents));
        }
    }
}
