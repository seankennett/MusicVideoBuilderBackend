﻿using Dapper;
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

        public async Task<IEnumerable<UserLayer>> GetAllCompleteAsync(Guid userObjectId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                var userLayers = await connection.QueryAsync<UserLayerDTO>("GetUserLayersByBuildStatus", new { userObjectId, BuildStatusId = BuildStatus.Complete }, commandType: CommandType.StoredProcedure);
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