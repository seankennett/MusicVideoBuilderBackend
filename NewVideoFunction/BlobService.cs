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
        private readonly string _privateConnectionString;
        public BlobService(IOptions<Connections> options)
        {
            _privateConnectionString = options.Value.PrivateStorageConnectionString;
        }

        public async Task CleanTempFiles(string containerName, string blobPrefix)
        {
            var blobContainerClient = new BlobContainerClient(_privateConnectionString, containerName);
            var blobs = blobContainerClient.GetBlobsAsync(Azure.Storage.Blobs.Models.BlobTraits.None, Azure.Storage.Blobs.Models.BlobStates.None, $"{blobPrefix}/{SharedConstants.TempBlobPrefix}");
            await foreach (var blob in blobs)
            {
                var blobClient = blobContainerClient.GetBlobClient(blob.Name);
                await blobClient.DeleteAsync();
            }
        }

        public async Task<Uri> GetBlobSas(string containerName, string blobName)
        {
            var blobContainerClient = new BlobClient(_privateConnectionString, containerName, blobName);
            var properties = await blobContainerClient.GetPropertiesAsync();
            return blobContainerClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, properties.Value.CreatedOn.AddDays(28));
        }
    }
}
