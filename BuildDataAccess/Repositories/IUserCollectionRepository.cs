using BuildDataAccess.Entities;

namespace BuildDataAccess.Repositories
{
    public interface IUserCollectionRepository
    {
        Task<IEnumerable<UserCollection>> GetAllAsync(Guid userObjectId);
        Task SavePendingUserCollectionAsync(IEnumerable<Guid> uniqueLayers, Guid userObjectId, Guid buildId);
        Task ConfirmPendingCollections(Guid buildId);
    }
}