using BuildDataAccess.Entities;

namespace BuildDataAccess.Repositories
{
    public interface IUserCollectionRepository
    {
        Task<IEnumerable<UserCollection>> GetAllAsync(Guid userObjectId);
        Task SavePendingUserLayersAsync(IEnumerable<Guid> uniqueLayers, Guid userObjectId, Guid buildId);
        Task ConfirmPendingUserLayers(Guid buildId);
    }
}