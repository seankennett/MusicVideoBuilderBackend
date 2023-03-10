using SharedEntities.Models;

namespace SpaWebApi.Services
{
    public interface IUserLayerService
    {
        //Task DeleteAsync(Guid userObjectId, int userLayerId);
        Task<IEnumerable<UserLayer>> GetAllAsync(Guid userObjectId);
        //Task<UserLayer> SaveAsync(Guid userObjectId, Guid layerId);
        //Task<UserLayer> UpdateAsync(Guid userObjectId, UserLayer userLayer);
    }
}
