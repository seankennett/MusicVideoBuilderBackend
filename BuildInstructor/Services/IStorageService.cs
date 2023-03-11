using System;
using System.Threading.Tasks;

namespace BuildInstructor.Services
{
    public interface IStorageService
    {
        Uri GetContainerSasUri(string userContainerName, TimeSpan timeSpan);
        Task UploadTextFile(string containerName, string blobPrefix, string fileName, string contents, bool createContainer);
    }
}