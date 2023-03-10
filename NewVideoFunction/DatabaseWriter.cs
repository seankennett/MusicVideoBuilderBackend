using Dapper;
using Microsoft.Extensions.Options;
using NewVideoFunction.Interfaces;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace NewVideoFunction
{
    public class DatabaseWriter : IDatabaseWriter
    {
        private string _sqlConnection;

        public DatabaseWriter(IOptions<Connections> connections)
        {
            _sqlConnection = connections.Value.SqlConnectionString;
        }

        public async Task UpdateIsBuilding(int videoId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                await connection.ExecuteAsync("UpdateVideoIsBuilding", new { VideoId = videoId, IsBuilding = false }, commandType: System.Data.CommandType.StoredProcedure);
            }
        }
    }
}
