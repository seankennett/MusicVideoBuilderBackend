using SharedEntities.Models;

namespace DataAccessLayer.Repositories
{
    public interface IUserLayerRepository
    {
        Task<IEnumerable<UserLayer>> GetAllAsync(Guid userObjectId);
        Task SaveUserLayersAsync(IEnumerable<Guid> uniqueLayers, Guid userObjectId, Guid buildId);
    }
}