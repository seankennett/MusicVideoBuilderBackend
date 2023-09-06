using BuildDataAccess.DTOEntities;
using BuildDataAccess.Entities;
using BuildEntities;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;

namespace BuildDataAccess.Repositories
{
    public class UserDisplayLayerRepository : IUserDisplayLayerRepository
    {
        private readonly string _sqlConnection;

        public UserDisplayLayerRepository(IOptions<SqlConfig> connections)
        {
            _sqlConnection = connections.Value.DatabaseConnectionString;
        }

        public async Task ConfirmPendingUserLayers(Guid buildId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                await connection.ExecuteAsync("ConfirmPendingUserDisplayLayers", new { BuildId = buildId }, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task<IEnumerable<UserDisplayLayer>> GetAllAsync(Guid userObjectId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                var userLayers = await connection.QueryAsync<UserDisplayLayerDTO>("GetUserDisplayLayers", new { userObjectId }, commandType: CommandType.StoredProcedure);
                var pendingUserLayers = await connection.QueryAsync<UserDisplayLayerDTO>("GetPendingUserDisplayLayers", new { userObjectId }, commandType: CommandType.StoredProcedure);
                return userLayers.Concat(pendingUserLayers).Select(ul => new UserDisplayLayer
                {
                    DisplayLayerId = ul.DisplayLayerId,
                    UserDisplayLayerId = ul.UserDisplayLayerId,
                    License = (License)ul.LicenseId,
                    Resolution = (Resolution)ul.ResolutionId
                });
            }
        }

        public async Task SavePendingUserLayersAsync(IEnumerable<Guid> uniqueLayers, Guid userObjectId, Guid buildId)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("ForeignId");
            dataTable.Columns.Add("Order");
            for (short i = 0; i < uniqueLayers.Count(); i++)
            {
                dataTable.Rows.Add(uniqueLayers.ElementAt(i), i);
            }

            using (var connection = new SqlConnection(_sqlConnection))
            {
                await connection.ExecuteAsync("InsertPendingUserDisplayLayers", new
                {
                    userObjectId,
                    BuildId = buildId,
                    Layers = dataTable.AsTableValuedParameter("GuidOrderType"),

                }, commandType: CommandType.StoredProcedure);

            }
        }
    }
}