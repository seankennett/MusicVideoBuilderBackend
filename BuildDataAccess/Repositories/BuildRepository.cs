
using System.Data;
using Dapper;
using SharedEntities.Models;
using Microsoft.Extensions.Options;
using BuildDataAccess.DTOEntities;
using BuildDataAccess.Entities;
using BuildEntities;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Server;
using VideoEntities.Entities;

namespace BuildDataAccess.Repositories
{
    public class BuildRepository : IBuildRepository
    {
        private readonly string _sqlConnection;

        public BuildRepository(IOptions<SqlConfig> connections)
        {
            _sqlConnection = connections.Value.DatabaseConnectionString;
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
                    VideoName = b.VideoName,
                    Format = (Formats)b.FormatId,
                    HasAudio = b.HasAudio,
                    PaymentIntentId = b.PaymentIntentId,
                    DateUpdated = b.DateUpdated
                });
            }
        }

        public async Task<UserBuild?> GetAsync(Guid buildId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                var buildDto = await connection.QueryFirstOrDefaultAsync<BuildDTO?>("GetUserBuild", new { BuildId = buildId }, commandType: CommandType.StoredProcedure);
                return buildDto != null && buildDto.UserObjectId.HasValue ? new UserBuild
                {
                    License = (License)buildDto.LicenseId,
                    Resolution = (Resolution)buildDto.ResolutionId,
                    BuildId = buildDto.BuildId,
                    BuildStatus = (BuildStatus)buildDto.BuildStatusId,
                    VideoId = buildDto.VideoId,
                    VideoName = buildDto.VideoName,
                    Format = (Formats)buildDto.FormatId,
                    HasAudio = buildDto.HasAudio,
                    PaymentIntentId = buildDto.PaymentIntentId,
                    DateUpdated = buildDto.DateUpdated,
                    UserObjectId = buildDto.UserObjectId.Value
                } : null;
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
                    VideoName = buildDto.VideoName,
                    Format = (Formats)buildDto.FormatId,
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
                    build.VideoName,
                    FormatId = (short)build.Format,
                    BuildStatusId = (short)build.BuildStatus,
                    ResolutionId = (short)build.Resolution,
                    LicenseId = (short)build.License,
                    UserObjectId = userObjectId,
                    build.PaymentIntentId,
                    build.HasAudio,
                }, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
