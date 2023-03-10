using SharedEntities.Models;

namespace SpaWebApi.Services
{
    public interface IClipService
    {
        Task DeleteAsync(Guid userObjectId, int clipId);
        Task<IEnumerable<Clip>> GetAllAsync(Guid userObjectId);
        Task<Clip> SaveAsync(Guid userObjectId, Clip clip);
    }
}
