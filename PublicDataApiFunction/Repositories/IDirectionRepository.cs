using CollectionEntities.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PublicDataApiFunction.Repositories
{
    public interface IDirectionRepository
    {
        Task<IEnumerable<Direction>> GetAllDirections();
    }
}