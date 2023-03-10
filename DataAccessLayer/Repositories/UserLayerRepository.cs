using Dapper;
using DataAccessLayer.DTOEntities;
using Microsoft.Extensions.Options;
using SharedEntities.Models;
using System.Data;
using System.Data.SqlClient;

namespace DataAccessLayer.Repositories
{
    public class UserLayerRepository : IUserLayerRepository
    {
        private readonly string _sqlConnection;

        public UserLayerRepository(IOptions<Connections> connections)
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

        //public async Task<UserLayer> SaveAsync(Guid userObjectId, Guid layerId)
        //{
        //    using (var connection = new SqlConnection(_sqlConnection))
        //    {
        //        var ul = await connection.QueryFirstAsync<UserLayerDTO>("InsertUserLayer", new { userObjectId, layerId, userLayerStatusId = UserLayerStatus.Saved }, commandType: CommandType.StoredProcedure);
        //        return new UserLayer
        //        {
        //            DateUpdated = ul.DateUpdated,
        //            LayerId = ul.LayerId,
        //            LayerName = ul.LayerName,
        //            LayerType = (LayerTypes)ul.LayerTypeId,
        //            UserLayerId = ul.UserLayerId,
        //            UserLayerStatus = (UserLayerStatus)ul.UserLayerStatusId
        //        };
        //    }
        //}

        //public async Task<UserLayerDTO> GetAsync(Guid userObjectId, int userLayerId)
        //{
        //    using (var connection = new SqlConnection(_sqlConnection))
        //    {
        //        return await connection.QueryFirstOrDefaultAsync<UserLayerDTO>("GetUserLayer", new { userObjectId, UserLayerId = userLayerId }, commandType: CommandType.StoredProcedure);                
        //    }
        //}

        //public async Task DeleteAsync(int userLayerId)
        //{         
        //    using (var connection = new SqlConnection(_sqlConnection))
        //    {
        //        await connection.ExecuteAsync("DeleteUserLayer", new { UserLayerId = userLayerId }, commandType: CommandType.StoredProcedure);
        //    }
        //}

        //public async Task<UserLayer> UpdateAsync(int userLayerId, UserLayerStatus userLayerStatus)
        //{
        //    using (var connection = new SqlConnection(_sqlConnection))
        //    {
        //        var ul = await connection.QueryFirstAsync<UserLayerDTO>("UpdateUserLayer", new { userLayerId, userLayerStatusId = userLayerStatus }, commandType: CommandType.StoredProcedure);
        //        return new UserLayer
        //        {
        //            DateUpdated = ul.DateUpdated,
        //            LayerId = ul.LayerId,
        //            LayerName = ul.LayerName,
        //            LayerType = (LayerTypes)ul.LayerTypeId,
        //            UserLayerId = ul.UserLayerId,
        //            UserLayerStatus = (UserLayerStatus)ul.UserLayerStatusId
        //        };
        //    }
        //}
    }
}