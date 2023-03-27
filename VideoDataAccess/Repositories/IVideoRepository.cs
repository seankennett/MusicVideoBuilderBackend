using VideoDataAccess.Entities;

namespace VideoDataAccess.Repositories
{
    public interface IVideoRepository
    {
        Task<IEnumerable<Video>> GetAllAsync(Guid userObjectId);
        Task<Video?> GetAsync(Guid userObjectId, int videoId);
        Task DeleteAsync(int videoId);
        Task<Video> SaveAsync(Guid userObjectId, Video video);
    }
}