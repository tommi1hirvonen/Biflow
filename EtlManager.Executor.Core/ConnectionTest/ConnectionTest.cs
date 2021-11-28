using EtlManager.Executor.Core.Common;
using Microsoft.Data.SqlClient;

namespace EtlManager.Executor.Core.ConnectionTest;

public class ConnectionTest : IConnectionTest
{
    private readonly IExecutionConfiguration _executionConfiguration;

    public ConnectionTest(IExecutionConfiguration executionConfiguration)
    {
        _executionConfiguration = executionConfiguration;
    }

    public async Task RunAsync()
    {
        try
        {
            var connectionString = _executionConfiguration.ConnectionString;
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            Console.WriteLine("Connection test succeeded.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection test failed.\n{ex.Message}");
        }
    }
}
