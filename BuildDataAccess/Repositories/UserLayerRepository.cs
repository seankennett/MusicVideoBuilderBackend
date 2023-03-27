using BuildDataAccess.DTOEntities;
using BuildDataAccess.Entities;
using BuildEntities;
using Dapper;
using Microsoft.Extensions.Options;
using System.Data;
using System.Data.SqlClient;

namespace BuildDataAccess.Repositories
{
    public class UserLayerRepository : IUserLayerRepository
    {
        private readonly string _sqlConnection;

        public UserLayerRepository(IOptions<SqlConfig> connections)
        {
            _sqlConnection = connections.Value.SqlConnectionString;
        }

        public async Task<IEnumerable<UserLayer>> GetAllAsync(Guid userObjectId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                var userLayers = await connection.QueryAsync<UserLayerDTO>("GetUserLayers", new { userObjectId }, commandType: CommandType.StoredProcedure);
                return userLayers.Select(ul => new UserLayer
                {
                    LayerId = ul.LayerId,
                    UserLayerId = ul.UserLayerId,
                    License = (License)ul.LicenseId,
                    Resolution = (Resolution)ul.ResolutionId,
                    LayerName = ul.LayerName
                });
            }
        }

        public async Task SaveUserLayersAsync(IEnumerable<Guid> uniqueLayers, Guid userObjectId, Guid buildId)
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
                await connection.ExecuteAsync("InsertUserLayers", new
                {
                    userObjectId,
                    BuildId = buildId,
                    Layers = dataTable.AsTableValuedParameter("GuidOrderType"),

                }, commandType: CommandType.StoredProcedure);

            }
        }
    }
}