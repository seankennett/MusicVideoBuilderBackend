using System;
using System.Threading.Tasks;

namespace NewVideoFunction.Interfaces
{
    public interface IBlobService
    {
        Task CleanTempFiles(string containerName, string blobPrefix);
        Task<Uri> GetBlobSas(string containerName, string blobName);
    }
}
