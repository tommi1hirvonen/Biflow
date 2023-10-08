using Biflow.Core;

namespace Biflow.Executor.Core.ConnectionTest;

internal class ConnectionTest(ISqlConnectionFactory sqlConnectionFactory) : IConnectionTest
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory = sqlConnectionFactory;

    public async Task RunAsync()
    {

        using var connection = _sqlConnectionFactory.Create();
        await connection.OpenAsync();
    }
}
