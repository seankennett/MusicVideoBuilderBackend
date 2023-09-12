using CollectionEntities.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PublicDataApiFunction.Repositories
{
    public class DirectionRepository : IDirectionRepository
    {
        private readonly string _sqlConnection;
        private readonly IMemoryCache _memoryCache;
        private const string GetAllCacheKey = "GetAllDirections";

        public DirectionRepository(IOptions<SqlConfig> connections, IMemoryCache memoryCache)
        {
            _sqlConnection = connections.Value.DatabaseConnectionString;
            _memoryCache = memoryCache;
        }
        public async Task<IEnumerable<Direction>> GetAllDirections()
        {
            IEnumerable<Direction> directions;
            if (_memoryCache.TryGetValue(GetAllCacheKey, out directions))
            {
                return directions;
            }

            using (var connection = new SqlConnection(_sqlConnection))
            {
                directions = await connection.QueryAsync<Direction>("GetDirections", commandType: CommandType.StoredProcedure);
            }
            _memoryCache.Set(GetAllCacheKey, directions, TimeSpan.FromHours(1));

            return directions;
        }
    }
}
