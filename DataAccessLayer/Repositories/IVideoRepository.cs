using SharedEntities.Models;

namespace DataAccessLayer.Repositories
{
    public interface IVideoRepository
    {
        Task<IEnumerable<Video>> GetAllAsync(Guid userObjectId);
        Task<Video?> GetAsync(Guid userObjectId, int videoId);
        Task DeleteAsync(int videoId);
        Task<Video> SaveAsync(Guid userObjectId, Video video);
    }
}