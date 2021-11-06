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
                catalogConnectionString = await context.SqlConnections
                    .AsNoTracking()
                    .Where(c => c.ConnectionId == connectionId)
                    .Select(c => c.ConnectionString)
                    .FirstOrDefaultAsync() ?? throw new ArgumentNullException(nameof(catalogConnectionString), "Connection string was null");
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
                connectionString = await context.SqlConnections
                    .AsNoTracking()
                    .Where(c => c.ConnectionId == connectionId)
                    .Select(c => c.ConnectionString)
                    .FirstOrDefaultAsync() ?? throw new ArgumentNullException(nameof(connectionString), "Connection string was null");
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

        public async Task<List<(string AgentJobName, bool IsEnabled)>> GetAgentJobsAsync(Guid connectionId)
        {
            string connectionString;
            using (var context = _dbContextFactory.CreateDbContext())
            {
                connectionString = await context.SqlConnections
                    .AsNoTracking()
                    .Where(c => c.ConnectionId == connectionId)
                    .Select(c => c.ConnectionString)
                    .FirstOrDefaultAsync() ?? throw new ArgumentNullException(nameof(connectionString), "Connection string was null");
            }
            using var sqlConnection = new SqlConnection(connectionString);
            var rows = await sqlConnection.QueryAsync<dynamic>("EXEC msdb.dbo.sp_help_job");
            var agentJobs = rows.Select(r => ((string)r.name, ((short)r.enabled) > 0)).ToList();
            return agentJobs;
        }

        public async Task<List<AsModel>> GetAnalysisServicesModelsAsync(Guid connectionId)
        {
            string connectionString;
            using (var context = _dbContextFactory.CreateDbContext())
            {
                connectionString = await context.AnalysisServicesConnections
                    .AsNoTracking()
                    .Where(c => c.ConnectionId == connectionId)
                    .Select(c => c.ConnectionString)
                    .FirstOrDefaultAsync() ?? throw new ArgumentNullException(nameof(connectionString), "Connection string was null");
            }

            var models = new List<AsModel>();
            await Task.Run(() =>
            {
                using var server = new Microsoft.AnalysisServices.Tabular.Server();
                server.Connect(connectionString);
                var databases = server.Databases;
                for (int dbi = 0; dbi < databases.Count; dbi++)
                {
                    var database = databases[dbi];
                    var model = database.Model;
                    var asModel = new AsModel(database.Name, new());
                    for (int tbi = 0; tbi < model.Tables.Count; tbi++)
                    {
                        var table = model.Tables[tbi];
                        var asTable = new AsTable(table.Name, asModel, new());
                        for (int pi = 0; pi < table.Partitions.Count; pi++)
                        {
                            var partition = table.Partitions[pi];
                            var asPartition = new AsPartition(partition.Name, asTable);
                            asTable.Partitions.Add(asPartition);
                        }
                        asModel.Tables.Add(asTable);
                    }
                    models.Add(asModel);
                }
            });
            return models;
        }

    }

    public record AsModel(string ModelName, List<AsTable> Tables);

    public record AsTable(string TableName, AsModel Model, List<AsPartition> Partitions);

    public record AsPartition(string PartitionName, AsTable Table);

}
