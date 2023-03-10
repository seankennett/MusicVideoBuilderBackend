using System.Threading.Tasks;

namespace NewVideoFunction.Interfaces
{
    public interface IDatabaseWriter
    {
        Task UpdateIsBuilding(int videoId);
    }
}