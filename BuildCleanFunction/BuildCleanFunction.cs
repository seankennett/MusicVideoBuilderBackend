using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildCleanFunction
{
    public class BuildCleanFunction
    {
        private readonly string _sqlConnection;
        public BuildCleanFunction(IOptions<SqlConfig> sqlConfig)
        {
            _sqlConnection = sqlConfig.Value.DatabaseConnectionString;
        }

        [FunctionName("BuildCleanFunction")]
        public async Task Run([TimerTrigger("0 0 3 * * *", RunOnStartup = true)]TimerInfo myTimer)
        {
            using (var connection = new SqlConnection(_sqlConnection))
            {
                await connection.ExecuteAsync("[dbo].[CleanUpBuilds]", commandType: System.Data.CommandType.StoredProcedure);
            }
        }
    }
}
