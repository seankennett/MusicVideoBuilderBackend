using VideoDataAccess.Entities;

namespace VideoDataAccess.Repositories
{
    public interface IClipRepository
    {
        Task DeleteAsync(int clipId);
        Task<IEnumerable<Clip>> GetAllAsync(Guid userObjectId);
        Task<Clip> SaveAsync(Guid userObjectId, Clip clip);
        Task<Clip?> GetAsync(Guid userObjectId, int clipId);
    }
}