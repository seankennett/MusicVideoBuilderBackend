using BuildDataAccess.Entities;

namespace BuildDataAccess.Repositories
{
    public interface IBuildRepository
    {
        Task<IEnumerable<Build>> GetAllAsync(Guid userObjectId);
        Task<IEnumerable<Build>> GetAllByVideoIdAsync(Guid userObjectId, int videoId);
        Task<UserBuild?> GetAsync(Guid buildId);
        Task<UserBuild?> GetByPaymentIntentId(string paymentIntentId);
        Task SaveAsync(Build build, Guid userObjectId);
    }
}