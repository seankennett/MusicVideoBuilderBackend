using CollectionEntities.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PublicDataApiFunction.Repositories
{
    public interface ICollectionRepository
    {
        Task<IEnumerable<Collection>> GetAllCollectionsAsync();
    }
}