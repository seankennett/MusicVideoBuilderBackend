using SharedEntities.Models;

namespace DataAccessLayer.Repositories
{
    public interface ILayerRepository
    {
        Task<IEnumerable<LayerFinder>> GetAllLayerFinderAsync();
    }
}