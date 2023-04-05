using BuildDataAccess.Entities;

namespace BuildDataAccess.Repositories
{
    public interface IUserLayerRepository
    {
        Task<IEnumerable<UserLayer>> GetAllAsync(Guid userObjectId);
        Task SavePendingUserLayersAsync(IEnumerable<Guid> uniqueLayers, Guid userObjectId, Guid buildId);
        Task ConfirmPendingUserLayers(Guid buildId);
    }
}