using LayerDataAccess.Entities;

namespace LayerDataAccess.Repositories
{
    public interface IDirectionRepository
    {
        Task<IEnumerable<Direction>> GetAllDirections();
    }
}