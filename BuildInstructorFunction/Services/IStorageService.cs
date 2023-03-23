using SharedEntities.Models;
using System;
using System.Threading.Tasks;

namespace BuildInstructorFunction.Services
{
    public interface IStorageService
    {
        Uri GetContainerSasUri(string userContainerName, TimeSpan timeSpan);
        Task SendFreeBuilderMessageAsync(BuilderMessage builderMessage);
        Task SendHdBuilderMessageAsync(BuilderMessage builderMessage);
        Task UploadTextFile(string containerName, string blobPrefix, string fileName, string contents, bool createContainer);
    }
}