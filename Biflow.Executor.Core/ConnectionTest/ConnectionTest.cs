using Biflow.Core;

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

        using var connection = _sqlConnectionFactory.Create();
        await connection.OpenAsync();
    }
}
