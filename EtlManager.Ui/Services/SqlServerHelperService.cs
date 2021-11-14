using Dapper;
using EtlManager.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Ui;

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

    public async Task<List<StoredProcedure>> GetStoredProcedures(Guid connectionId)
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
        var sql = @"
            select
	            ProcedureId = a.object_id,
	            SchemaName = b.name,
	            ProcedureName = a.name,
	            ParameterId = c.parameter_id,
	            ParameterName = c.name,
	            ParameterType = TYPE_NAME(c.user_type_id)
            from sys.procedures as a
	            inner join sys.schemas as b on a.schema_id = b.schema_id
	            left join sys.parameters as c on a.object_id = c.object_id
            order by
	            SchemaName,
	            ProcedureName,
	            ParameterId";
        var procedures = new Dictionary<int, StoredProcedure>();
        var data = await sqlConnection.QueryAsync<StoredProcedure, StoredProcedureParameter, StoredProcedure>(
            sql,
            (proc, param) =>
            {
                if (!procedures.TryGetValue(proc.ProcedureId, out var storedProc))
                {
                    storedProc = proc;
                    procedures[storedProc.ProcedureId] = storedProc;
                }
                if (param is not null)
                {
                    storedProc.Parameters.Add(param);
                }
                return storedProc;
            },
            splitOn: "ParameterId");
        return procedures.Values.ToList();
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

    public async Task<IEnumerable<(string Schema, string Name, string Type)?>> GetSqlModulesAsync(Guid connectionId)
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
        var results = await sqlConnection.QueryAsync<(string, string, string)?>(
            @"select
                SchemaName = c.name,
                ObjectName = b.name,
                ObjectType = b.type_desc
            from sys.sql_modules as a
            join sys.objects as b on a.object_id = b.object_id
                join sys.schemas as c on b.schema_id = c.schema_id
            order by
                SchemaName,
                ObjectName");
        return results;
    }

    public async Task<string> GetObjectDefinitionAsync(Guid connectionId, string objectName)
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
        var definition = await sqlConnection.ExecuteScalarAsync<string>(
            "SELECT OBJECT_DEFINITION(OBJECT_ID(@ObjectName))",
            new { ObjectName = objectName});
        return definition;
    }

    public async Task<IEnumerable<SqlReference>> GetSqlReferencedObjectsAsync(
        Guid connectionId,
        string referencingSchemaOperator,
        string referencingSchemaFilter,
        string referencingNameOperator,
        string referencingNameFilter,
        string referencedSchemaOperator,
        string referencedSchemaFilter,
        string referencedNameOperator,
        string referencedNameFilter)
    {
        if (referencingSchemaOperator != "=" && referencingSchemaOperator != "like")
            throw new ArgumentException($"Invalid operator {referencingSchemaOperator}", nameof(referencingSchemaOperator));
        if (referencingNameOperator != "=" && referencingNameOperator != "like")
            throw new ArgumentException($"Invalid operator {referencingNameOperator}", nameof(referencingNameOperator));
        if (referencedSchemaOperator != "=" && referencedSchemaOperator != "like")
            throw new ArgumentException($"Invalid operator {referencedSchemaOperator}", nameof(referencedSchemaOperator));
        if (referencedNameOperator != "=" && referencedNameOperator != "like")
            throw new ArgumentException($"Invalid operator {referencedNameOperator}", nameof(referencedNameOperator));

        string connectionString;
        using (var context = _dbContextFactory.CreateDbContext())
        {
            connectionString = await context.SqlConnections
                .AsNoTracking()
                .Where(c => c.ConnectionId == connectionId)
                .Select(c => c.ConnectionString)
                .FirstOrDefaultAsync() ?? throw new ArgumentNullException(nameof(connectionString), "Connection string was null");
        }

        static string encodeForLike(string term) => term.Replace("[", "[[]").Replace("%", "[%]");
        var encodedReferencingSchemaFilter = referencingSchemaOperator == "=" ? referencingSchemaFilter : $"%{encodeForLike(referencingSchemaFilter)}%";
        var encodedReferencingNameFilter = referencingNameOperator == "=" ? referencingNameFilter : $"%{encodeForLike(referencingNameFilter)}%";
        var encodedReferencedSchemaFilter = referencedSchemaOperator == "=" ? referencedSchemaFilter : $"%{encodeForLike(referencedSchemaFilter)}%";
        var encodedReferencedNameFilter = referencedNameOperator == "=" ? referencedNameFilter :  $"%{encodeForLike(referencedNameFilter)}%";

        using var sqlConnection = new SqlConnection(connectionString);
        var rows = await sqlConnection.QueryAsync<SqlReference>(
            $@"select distinct
	            ReferencingSchema = c.name,
	            ReferencingName = b.name,
	            ReferencingType = b.type_desc,
	            ReferencedSchema = e.name,
	            ReferencedName = d.name,
	            ReferencedType = d.type_desc
            from sys.sql_expression_dependencies as a
	            join sys.objects as b on a.referencing_id = b.object_id
	            join sys.schemas as c on b.schema_id = c.schema_id
	            join sys.objects as d on a.referenced_id = d.object_id
	            join sys.schemas as e on d.schema_id = e.schema_id
            where
                c.name {referencingSchemaOperator} @ReferencingSchemaFilter and
                b.name {referencingNameOperator} @ReferencingNameFilter and
                e.name {referencedSchemaOperator} @ReferencedSchemaFilter and
                d.name {referencedNameOperator} @ReferencedNameFilter
            order by
	            ReferencingSchema,
	            ReferencingName,
	            ReferencedSchema,
	            ReferencedName", new
            {
                ReferencingSchemaFilter = encodedReferencingSchemaFilter,
                ReferencingNameFilter = encodedReferencingNameFilter,
                ReferencedSchemaFilter = encodedReferencedSchemaFilter,
                ReferencedNameFilter = encodedReferencedNameFilter
            });
        return rows;
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

public class StoredProcedure
{
    public StoredProcedure(int procedureId, string schemaName, string procedureName)
    {
        ProcedureId = procedureId;
        SchemaName = schemaName;
        ProcedureName = procedureName;
    }
    public int ProcedureId { get; }
    public string SchemaName { get; }
    public string ProcedureName { get; }
    public List<StoredProcedureParameter> Parameters { get; } = new();
}

public class StoredProcedureParameter
{
    public StoredProcedureParameter(int parameterId, string parameterName, string parameterType)
    {
        ParameterId = parameterId;
        ParameterName = parameterName; 
        ParameterType = parameterType;
    }
    public int ParameterId { get; }
    public string ParameterName { get; }
    public string ParameterType { get; }
}

public record SqlReference(
    string ReferencingSchema,
    string ReferencingName,
    string ReferencingType,
    string ReferencedSchema,
    string ReferencedName,
    string ReferencedType);