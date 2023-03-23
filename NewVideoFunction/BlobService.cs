using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using NewVideoFunction.Interfaces;
using SharedEntities;
using System;
using System.Threading.Tasks;

namespace NewVideoFunction
{
    public class BlobService : IBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobService(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        public async Task CleanTempFiles(string containerName, string blobPrefix)
        {
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobs = blobContainerClient.GetBlobsAsync(Azure.Storage.Blobs.Models.BlobTraits.None, Azure.Storage.Blobs.Models.BlobStates.None, $"{blobPrefix}/{SharedConstants.TempBlobPrefix}");
            await foreach (var blob in blobs)
            {
                var blobClient = blobContainerClient.GetBlobClient(blob.Name);
                await blobClient.DeleteAsync();
            }
        }

        public async Task<Uri> GetBlobSas(string containerName, string blobName)
        {
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = blobContainerClient.GetBlobClient(blobName);
            var properties = await blobClient.GetPropertiesAsync();
            return blobClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, properties.Value.CreatedOn.AddDays(28));
        }
    }
}
