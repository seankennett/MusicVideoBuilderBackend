﻿using Dapper;
using Microsoft.Extensions.Options;
using SharedEntities.Models;
using System.Data.SqlClient;

namespace DataAccessLayer.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly string _sqlConnection;

        public TagRepository(IOptions<SqlConfig> connections)
        {
            _sqlConnection = connections.Value.SqlConnectionString;
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
