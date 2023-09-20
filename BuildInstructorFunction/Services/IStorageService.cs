using BuilderEntities.Entities;
using System;
using System.Threading.Tasks;

namespace BuildInstructorFunction.Services
{
    public interface IStorageService
    {
        Uri GetContainerUri(string userContainerName);
        Task SendFreeBuilderMessageAsync(BuilderMessage builderMessage);
        Task UploadTextFile(string containerName, string blobPrefix, string fileName, string contents, bool createContainer);
    }
}