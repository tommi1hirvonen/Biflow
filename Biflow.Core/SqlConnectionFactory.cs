using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace Biflow.Core;

internal class SqlConnectionFactory(IConfiguration configuration) : ISqlConnectionFactory
{
    private readonly string _connectionString = configuration.GetConnectionString("AppDbContext")
        ?? throw new ApplicationException("Connection string not found");

    public DbConnection Create() => new SqlConnection(_connectionString);
}
