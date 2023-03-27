using LayerDataAccess.Entities;
using SpaWebApi.Models;

namespace SpaWebApi.Services
{
    public interface ILayerUploadService
    {
        Task<LayerUploadContainer> CreateLayerUploadContainer();
        Task SendToImageProcessingFunction(Guid userObjectId, LayerUploadMessage layerUpload);
    }
}