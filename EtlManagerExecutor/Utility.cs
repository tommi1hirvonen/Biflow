using Dapper;
using Serilog;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{

    public static class Utility
    {
        public static async Task UpdateErrorMessageAsync(ExecutionConfiguration executionConfig, string errorMessage)
        {
            try
            {
                using var sqlConnection = new SqlConnection(executionConfig.ConnectionString);
                await sqlConnection.ExecuteAsync(
                    @"UPDATE etlmanager.Execution
                    SET ExecutionStatus = 'FAILED', ErrorMessage = @ErrorMessage, StartDateTime = GETDATE(), EndDateTime = GETDATE()
                    WHERE ExecutionId = @ExecutionId",
                    new { ErrorMessage = errorMessage, executionConfig.ExecutionId });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} Error updating error message", executionConfig.ExecutionId);
            }
        }
    }
}
