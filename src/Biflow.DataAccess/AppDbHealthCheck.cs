using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Biflow.DataAccess;

internal class AppDbHealthCheck(string connectionString) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connStrBuilder = new SqlConnectionStringBuilder
            {
                ConnectionString = connectionString,
                ConnectTimeout = 8 // Use a fixed connection timeout
            };
            await using var connection = new SqlConnection(connStrBuilder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand("SELECT 1", connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return HealthCheckResult.Healthy(
                description: "App database connection test successful",
                data: new Dictionary<string, object> { { "LastChecked", DateTimeOffset.UtcNow } });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                description: "App database connection test failed",
                exception: ex,
                data: new Dictionary<string, object> { { "LastChecked", DateTimeOffset.UtcNow } });
        }
    }
}