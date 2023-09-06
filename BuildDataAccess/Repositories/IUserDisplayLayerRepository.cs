using BuildDataAccess.Entities;

namespace BuildDataAccess.Repositories
{
    public interface IUserDisplayLayerRepository
    {
        Task<IEnumerable<UserDisplayLayer>> GetAllAsync(Guid userObjectId);
        Task SavePendingUserLayersAsync(IEnumerable<Guid> uniqueLayers, Guid userObjectId, Guid buildId);
        Task ConfirmPendingUserLayers(Guid buildId);
    }
}