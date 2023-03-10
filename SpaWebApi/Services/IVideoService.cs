using SharedEntities.Models;

namespace SpaWebApi.Services
{
    public interface IVideoService
    {
        Task<IEnumerable<Video>> GetAllAsync(Guid userObjectId);
        Task DeleteAsync(Guid userObjectId, int videoId);
        Task<Video> SaveAsync(Guid userObjectId, Video video);
    }
}
