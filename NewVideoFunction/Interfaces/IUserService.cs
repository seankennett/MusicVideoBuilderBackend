using System.Threading.Tasks;

namespace NewVideoFunction.Interfaces
{
    public interface IUserService
    {
        Task<(string username, string email)> GetDetails(string userObjectId);
    }
}