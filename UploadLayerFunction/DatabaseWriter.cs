using Dapper;
using Microsoft.Extensions.Options;
using SharedEntities.Models;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using UploadLayerFunction.Interfaces;


namespace UploadLayerFunction
{
    public class DatabaseWriter : IDatabaseWriter
    {
        private string _sqlConnection;

        public DatabaseWriter(IOptions<SqlConfig> connections)
        {
            _sqlConnection = connections.Value.SqlConnectionString;
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
