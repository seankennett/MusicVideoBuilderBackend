using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserSubscriptionAccess.Models;

namespace UserSubscriptionAccess.Repositories
{
    public class UserSubscriptionRepository : IUserSubscriptionRepository
    {
        private readonly string _sqlConnection;

        public UserSubscriptionRepository(IOptions<SqlConfig> connections)
        {
            _sqlConnection = connections.Value.DatabaseConnectionString;
        }

        public async Task DeleteByCustomerIdAsync(string customerId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                await connection.ExecuteAsync("DeleteUserSubscriptionByCustomerId", new { CustomerId = customerId }, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task<UserSubscription?> GetAsync(Guid userObjectId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                return await connection.QueryFirstOrDefaultAsync<UserSubscription>("GetUserSubscription", new { UserObjectId = userObjectId }, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task<UserSubscription> SaveAsync(UserSubscription userSubscription, Guid userObjectId)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                await connection.ExecuteAsync("UpsertUserSubscription", new
                {
                    UserObjectId = userObjectId,
                    userSubscription.SubscriptionId,
                    userSubscription.SubscriptionStatus,
                    userSubscription.CustomerId,
                    userSubscription.ProductId,
                }, commandType: CommandType.StoredProcedure);

                return userSubscription;
            }
        }
    }
}
