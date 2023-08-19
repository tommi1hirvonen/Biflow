using Biflow.Core;
using Biflow.Executor.Core.Common;
using Microsoft.Data.SqlClient;

namespace Biflow.Executor.Core.ConnectionTest;

internal class ConnectionTest : IConnectionTest
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public ConnectionTest(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public async Task RunAsync()
    {
        try
        {
            using var connection = _sqlConnectionFactory.Create();
            await connection.OpenAsync();
            Console.WriteLine("Connection test succeeded.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection test failed.\n{ex.Message}");
        }
    }
}
