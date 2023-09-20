using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using System.Text.Json;
using BuilderEntities.Entities;

namespace BuildInstructorFunction.Services
{
    public class StorageService : IStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly QueueClient _queueClientFree;

        public StorageService(BlobServiceClient blobServiceClient, QueueServiceClient queueServiceClient, IOptions<InstructorConfig> config)
        {
            _blobServiceClient = blobServiceClient;
            _queueClientFree = queueServiceClient.GetQueueClient(config.Value.FreeBuilderQueueName);
        }

        public Uri GetContainerUri(string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            return containerClient.Uri;
        }

        public async Task SendFreeBuilderMessageAsync(BuilderMessage builderMessage)
        {
            await _queueClientFree.SendMessageAsync(JsonSerializer.Serialize(builderMessage));
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
