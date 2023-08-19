using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace Biflow.Core;

internal class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration) =>
        _connectionString = configuration.GetConnectionString("BiflowContext")
        ?? throw new ApplicationException("Connection string not found");

    public DbConnection Create() => new SqlConnection(_connectionString);
}
