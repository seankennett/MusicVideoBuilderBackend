using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using BuildDataAccess;
using NewVideoFunction.Interfaces;
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
            var blobs = blobContainerClient.GetBlobsAsync(Azure.Storage.Blobs.Models.BlobTraits.None, Azure.Storage.Blobs.Models.BlobStates.None, $"{blobPrefix}/{BuildDataAccessConstants.TempBlobPrefix}");
            await foreach (var blob in blobs)
            {
                var blobClient = blobContainerClient.GetBlobClient(blob.Name);
                await blobClient.DeleteAsync();
            }
        }

        public async Task<Uri> GetBlobSas(string containerName, string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            var properties = await blobClient.GetPropertiesAsync();
            var expiresOn = properties.Value.CreatedOn.AddDays(7);
            var startsOn = DateTimeOffset.UtcNow;

            var userDelegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(startsOn, expiresOn);
            var sasBuilder = new BlobSasBuilder
            {
                BlobName = blobName,
                BlobContainerName = containerClient.Name,
                Resource = "b",
                StartsOn = startsOn,
                ExpiresOn = expiresOn,
            };
            var uri = blobClient.Uri;

            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            var blobUriBuilder = new BlobUriBuilder(uri)
            {
                Sas = sasBuilder.ToSasQueryParameters(userDelegationKey, _blobServiceClient.AccountName)
            };

            return blobUriBuilder.ToUri();
        }
    }
}
