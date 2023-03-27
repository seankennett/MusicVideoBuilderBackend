using LayerDataAccess.Entities;

namespace LayerDataAccess.Repositories
{
    public interface ILayerRepository
    {
        Task<IEnumerable<LayerFinder>> GetAllLayerFinderAsync();
        Task InsertLayer(LayerUploadMessage layerUploadMessage);
    }
}