using LayerDataAccess.Entities;

namespace LayerDataAccess.Repositories
{
    public interface ICollectionRepository
    {
        Task<IEnumerable<Collection>> GetAllCollectionsAsync();
    }
}