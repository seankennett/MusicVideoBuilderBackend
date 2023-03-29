using Dapper;
using LayerDataAccess.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace SpaWebApi.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly string _sqlConnection;

        public TagRepository(IOptions<SqlConfig> connections)
        {
            _sqlConnection = connections.Value.DatabaseConnectionString;
        }

        public IEnumerable<Tag> GetAll()
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                return connection.Query<Tag>("GetTags", commandType: System.Data.CommandType.StoredProcedure);
            }
        }
    }
}
