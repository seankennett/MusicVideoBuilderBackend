using System.Threading.Tasks;

namespace BuildInstructorFunction.Services
{
    public interface IBuildService
    {
        Task InstructBuildAsync(string paymentIntentId);
        Task InstructBuildAsync(UserBuild userBuild);
    }
}