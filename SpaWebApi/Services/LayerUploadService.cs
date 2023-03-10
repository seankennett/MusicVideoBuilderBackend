using SharedEntities.Models;
using SpaWebApi.Models;

namespace SpaWebApi.Services
{
    public class LayerUploadService : ILayerUploadService
    {
        private readonly IStorageService _storageService;
        public LayerUploadService(IStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<LayerUploadContainer> CreateLayerUploadContainer()
        {
            var layerId = Guid.NewGuid();
            Uri containerSasUrl = await _storageService.CreateUploadContainerAsync(layerId);
            return new LayerUploadContainer { LayerId = layerId, ContainerSasUrl = containerSasUrl.ToString() };
        }

        public async Task SendToImageProcessingFunction(Guid userObjectId, LayerUploadMessage layerUpload)
        {
            layerUpload.AuthorObjectId = userObjectId;
            await _storageService.RemoveContainerPolicySendToQueueAsync(layerUpload);
        }
    }
}
