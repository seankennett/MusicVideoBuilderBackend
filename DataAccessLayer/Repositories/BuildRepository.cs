
using System.Data.SqlClient;
using System.Data;
using Dapper;
using SharedEntities.Models;
using DataAccessLayer.DTOEntities;
using Microsoft.Extensions.Options;

namespace DataAccessLayer.Repositories
{
    public class BuildRepository : IBuildRepository
    {
        private readonly string _sqlConnection;

        public BuildRepository(IOptions<Connections> connections)
        {
            _sqlConnection = connections.Value.SqlConnectionString;
        }

        public async Task<IEnumerable<Build>> GetAllAsync(Guid userObjectId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                var builds = await connection.QueryAsync<BuildDTO>("GetBuilds", new { userObjectId }, commandType: CommandType.StoredProcedure);
                return builds.Select(b => new Build
                {
                    License = (License)b.LicenseId,
                    Resolution = (Resolution)b.ResolutionId,
                    BuildId = b.BuildId,
                    BuildStatus = (BuildStatus)b.BuildStatusId,
                    VideoId = b.VideoId,
                    HasAudio = b.HasAudio,
                    PaymentIntentId = b.PaymentIntentId,
                    DateUpdated = b.DateUpdated
                });
            }
        }

        public async Task<UserBuild?> GetByPaymentIntentId(string paymentIntentId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                var buildDto = await connection.QueryFirstOrDefaultAsync<BuildDTO?>("GetUserBuildByPaymentIntentId", new { PaymentIntentId = paymentIntentId }, commandType: CommandType.StoredProcedure);
                return buildDto != null && buildDto.UserObjectId.HasValue ? new UserBuild
                {
                    License = (License)buildDto.LicenseId,
                    Resolution = (Resolution)buildDto.ResolutionId,
                    BuildId = buildDto.BuildId,
                    BuildStatus = (BuildStatus)buildDto.BuildStatusId,
                    VideoId = buildDto.VideoId,
                    HasAudio = buildDto.HasAudio,
                    PaymentIntentId = buildDto.PaymentIntentId,
                    DateUpdated = buildDto.DateUpdated,
                    UserObjectId = buildDto.UserObjectId.Value
                } : null;
            }
        }

        public async Task SaveAsync(Build build, Guid userObjectId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                await connection.ExecuteAsync("UpsertBuild", new
                {
                    build.BuildId,
                    build.VideoId,
                    BuildStatusId = (short)build.BuildStatus,
                    ResolutionId = (short)build.Resolution,
                    LicenseId = (short)build.License,
                    UserObjectId = userObjectId,
                    build.PaymentIntentId,
                    build.HasAudio
                }, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
