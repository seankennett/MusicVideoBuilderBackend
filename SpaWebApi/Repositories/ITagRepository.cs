using LayerDataAccess.Entities;

namespace SpaWebApi.Repositories
{
    public interface ITagRepository
    {
        IEnumerable<Tag> GetAll();
    }
}