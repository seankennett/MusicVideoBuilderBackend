using Dapper;
using LayerDataAccess.DTOEntities;
using LayerDataAccess.Entities;
using LayerEntities;
using Microsoft.Extensions.Options;
using System.Data;
using System.Data.SqlClient;

namespace LayerDataAccess.Repositories
{
    public class LayerRepository : ILayerRepository
    {
        private readonly string _sqlConnection;

        public LayerRepository(IOptions<SqlConfig> connections)
        {
            _sqlConnection = connections.Value.SqlConnectionString;
        }

        public async Task<IEnumerable<LayerFinder>> GetAllLayerFinderAsync()
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                var reader = await connection.QueryMultipleAsync("GetLayerFinders", commandType: System.Data.CommandType.StoredProcedure);
                var layers = await reader.ReadAsync<LayerFinderDTO>();
                var tags = await reader.ReadAsync<LayerTagDTO>();

                var groupedTags = tags.GroupBy(x => x.LayerId);

                return layers.Select(l => new LayerFinder
                {
                    DateUpdated = l.DateUpdated,
                    LayerId = l.LayerId,
                    LayerName = l.LayerName,
                    LayerType = (LayerTypes)l.LayerTypeId,
                    Tags = groupedTags.First(gt => gt.Key == l.LayerId).Select(l => l.TagName),
                    UserCount = l.UserCount
                });
            }
        }

        public async Task InsertLayer(LayerUploadMessage layerUploadMessage)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("TagName");
            foreach (var tag in layerUploadMessage.Tags)
            {
                dataTable.Rows.Add(tag);
            }

            using (var connection = new SqlConnection(_sqlConnection))
            {
                var affectedRows = await connection.ExecuteAsync("InsertLayer",
                    new { layerUploadMessage.LayerId, layerUploadMessage.LayerName, Tags = dataTable.AsTableValuedParameter("TagsType"), LayerTypeId = (byte)layerUploadMessage.LayerType, layerUploadMessage.AuthorObjectId },
                    commandType: CommandType.StoredProcedure);
            }
        }
    }
}
