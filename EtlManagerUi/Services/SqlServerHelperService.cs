using Dapper;
using EtlManagerDataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerUi
{
    public class SqlServerHelperService
    {
        private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;

        public SqlServerHelperService(IDbContextFactory<EtlManagerContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<Dictionary<string, Dictionary<string, List<string>>>> GetCatalogPackages(Guid connectionId)
        {
            string catalogConnectionString;
            using (var context = _dbContextFactory.CreateDbContext())
            {
                catalogConnectionString = await context.Connections
                    .AsNoTracking()
                    .Where(c => c.ConnectionId == connectionId)
                    .Select(c => c.ConnectionString)
                    .FirstAsync() ?? throw new ArgumentNullException(nameof(catalogConnectionString), "Connection string was null");
            }
            using var sqlConnection = new SqlConnection(catalogConnectionString);
            var rows = await sqlConnection.QueryAsync<(string, string?, string?)>(
                @"SELECT
	                    [folders].[name] AS FolderName,
	                    [projects].[name] AS ProjectName,
	                    [packages].[name] AS PackageName
                    FROM [SSISDB].[catalog].[folders]
	                    LEFT JOIN [SSISDB].[catalog].[projects] ON [folders].[folder_id] = [projects].[folder_id]
	                    LEFT JOIN [SSISDB].[catalog].[packages] ON [projects].[project_id] = [packages].[project_id]
                    ORDER BY FolderName, ProjectName, PackageName");
            var catalog = rows
                .GroupBy(key => key.Item1, element => (element.Item2, element.Item3))
                .ToDictionary(
                grouping => grouping.Key,
                grouping => grouping
                                .Where(x => x.Item1 is not null)
                                .GroupBy(key => key.Item1, element => element.Item2)
                                .ToDictionary(
                                    grouping_ => grouping_.Key ?? "",
                                    grouping_ => grouping_.Where(x => x is not null).Select(x => x ?? "").ToList()));
            return catalog;
        }

        public async Task<Dictionary<string, List<string>>> GetStoredProcedures(Guid connectionId)
        {
            string connectionString;
            using (var context = _dbContextFactory.CreateDbContext())
            {
                connectionString = await context.Connections
                    .AsNoTracking()
                    .Where(c => c.ConnectionId == connectionId)
                    .Select(c => c.ConnectionString)
                    .FirstAsync() ?? throw new ArgumentNullException(nameof(connectionString), "Connection string was null");
            }
            using var sqlConnection = new SqlConnection(connectionString);
            var rows = await sqlConnection.QueryAsync<(string, string)>(
                @"SELECT OBJECT_SCHEMA_NAME([object_id]) AS [schema], [name]
                    FROM [sys].[procedures]
                    ORDER BY [schema], [name]");
            var procedures = rows
                .GroupBy(key => key.Item1, element => element.Item2)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());
            return procedures;
        }

    }
}
