using System.Threading.Tasks;

namespace BuildInstructor.Services
{
    public interface IBuildService
    {
        Task InstructBuildAsync(string paymentIntentId);
        Task InstructBuildAsync(UserBuild userBuild);
    }
}