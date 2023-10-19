using VideoDataAccess.Entities;

namespace VideoDataAccess.Repositories
{
    public interface IClipRepository
    {
        Task DeleteAsync(int clipId);
        Task<IEnumerable<Clip>> GetAllAsync(Guid userObjectId);
        Task<IEnumerable<Clip>> GetAllByVideoIdAsync(Guid userObjectId, int videoId);
        Task<Clip> SaveAsync(Guid userObjectId, Clip clip);
        Task<Clip?> GetAsync(Guid userObjectId, int clipId);
    }
}