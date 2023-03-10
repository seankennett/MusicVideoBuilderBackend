using SharedEntities.Models;
using SpaWebApi.Models;

namespace SpaWebApi.Services
{
    public interface ILayerUploadService
    {
        Task<LayerUploadContainer> CreateLayerUploadContainer();
        Task SendToImageProcessingFunction(Guid userObjectId, LayerUploadMessage layerUpload);
    }
}