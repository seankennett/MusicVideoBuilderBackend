using SharedEntities.Models;

namespace DataAccessLayer.Repositories
{
    public interface IUserLayerRepository
    {
        //Task DeleteAsync(int userLayerId);
        Task<IEnumerable<UserLayer>> GetAllCompleteAsync(Guid userObjectId);
        Task SaveUserLayersAsync(IEnumerable<Guid> uniqueLayers, Guid userObjectId, Guid buildId);
        //Task<UserLayerDTO> GetAsync(Guid userObjectId, int userLayerId);
        //Task<UserLayer> SaveAsync(Guid userObjectId, Guid layerId);
        //Task<UserLayer> UpdateAsync(int userLayerId, UserLayerStatus userLayerStatus);
    }
}