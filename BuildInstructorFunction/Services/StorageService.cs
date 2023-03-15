using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace BuildInstructorFunction.Services
{
    public class StorageService : IStorageService
    {
        private readonly Lazy<BlobServiceClient> _blobServiceClient;
        public StorageService(IOptions<Connections> connections)
        {
            _blobServiceClient = new Lazy<BlobServiceClient>(() => new BlobServiceClient(connections.Value.PrivateStorageConnectionString));
        }

        public Uri GetContainerSasUri(string containerName, TimeSpan sasTokenLength)
        {
            var containerClient = _blobServiceClient.Value.GetBlobContainerClient(containerName);
            return containerClient.GenerateSasUri(BlobContainerSasPermissions.Write, DateTime.UtcNow.Add(sasTokenLength));
        }

        public async Task UploadTextFile(string containerName, string blobPrefix, string fileName, string contents, bool createContainer)
        {
            var containerClient = _blobServiceClient.Value.GetBlobContainerClient(containerName);
            if (createContainer)
            {
                await containerClient.CreateIfNotExistsAsync();
            }

            var blobClient = containerClient.GetBlobClient($"{blobPrefix}/{fileName}");
            await blobClient.UploadAsync(BinaryData.FromString(contents));
        }
    }
}
