using SharedEntities.Models;

namespace DataAccessLayer.Repositories
{
    public interface ITagRepository
    {
        IEnumerable<Tag> GetAll();
    }
}