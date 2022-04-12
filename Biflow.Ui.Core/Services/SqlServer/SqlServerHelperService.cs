using Dapper;
using Biflow.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

public class SqlServerHelperService
{
    private readonly IDbContextFactory<BiflowContext> _dbContextFactory;

    public SqlServerHelperService(IDbContextFactory<BiflowContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<SSISCatalog> GetCatalogPackages(Guid connectionId)
    {
        var catalogConnectionString = await GetSqlConnectionStringAsync(connectionId);
        ArgumentNullException.ThrowIfNull(catalogConnectionString);
        using var sqlConnection = new SqlConnection(catalogConnectionString);
        var folders = new Dictionary<long, CatalogFolder>();
        var rows = await sqlConnection.QueryAsync<CatalogFolder, CatalogProject?, CatalogPackage?, CatalogParameter?, CatalogFolder>(
            @"SELECT
	            FolderId = [folders].[folder_id],
	            FolderName = [folders].[name],
	            ProjectId = [projects].[project_id],
	            ProjectName = [projects].[name],
	            PackageId = [packages].[package_id],
	            PackageName = [packages].[name],
	            ParameterId = [object_parameters].[parameter_id],
	            ParameterName = [object_parameters].[parameter_name],
	            ParameterType = [object_parameters].[data_type],
	            DesignDefaultValue = [object_parameters].[design_default_value],
	            DefaultValue = [object_parameters].[default_value]
            FROM [SSISDB].[catalog].[folders]
	            LEFT JOIN [SSISDB].[catalog].[projects] ON [folders].[folder_id] = [projects].[folder_id]
	            LEFT JOIN [SSISDB].[catalog].[packages] ON [projects].[project_id] = [packages].[project_id]
	            LEFT JOIN [SSISDB].[catalog].[object_parameters] ON
		            [packages].[project_id] = [object_parameters].[project_id] AND
		            [packages].[name] = [object_parameters].[object_name] AND
		            [object_parameters].[object_type] = 30",
            (folder, project, package, param) =>
            {
                if (!folders.TryGetValue(folder.FolderId, out var folderEntry))
                {
                    folderEntry = folder;
                    folders[folderEntry.FolderId] = folderEntry;
                }
                if (project is not null)
                {
                    if (!folderEntry.Projects.TryGetValue(project.ProjectId, out var projectEntry))
                    {
                        projectEntry = project;
                        folderEntry.Projects[projectEntry.ProjectId] = projectEntry;
                    }
                    if (package is not null)
                    {
                        if (!projectEntry.Packages.TryGetValue(package.PackageId, out var packageEntry))
                        {
                            packageEntry = package;
                            projectEntry.Packages[packageEntry.PackageId] = packageEntry;
                        }
                        if (param is not null)
                        {
                            if (!packageEntry.Parameters.TryGetValue(param.ParameterId, out var paramEntry))
                            {
                                paramEntry = param;
                                packageEntry.Parameters[paramEntry.ParameterId] = paramEntry;
                            }
                        }
                    }
                }
                return folderEntry;
            },
            splitOn: "ProjectId,PackageId,ParameterId");
        return new SSISCatalog(folders);
    }

    public async Task<List<StoredProcedure>> GetStoredProcedures(Guid connectionId)
    {
        var connectionString = await GetSqlConnectionStringAsync(connectionId);
        ArgumentNullException.ThrowIfNull(connectionString);
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
        var data = await sqlConnection.QueryAsync<StoredProcedure, StoredProcedureParameter?, StoredProcedure>(
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
        var connectionString = await GetSqlConnectionStringAsync(connectionId);
        ArgumentNullException.ThrowIfNull(connectionString);
        using var sqlConnection = new SqlConnection(connectionString);
        var rows = await sqlConnection.QueryAsync<dynamic>("EXEC msdb.dbo.sp_help_job");
        var agentJobs = rows.Select(r => ((string)r.name, ((short)r.enabled) > 0)).ToList();
        return agentJobs;
    }

    public async Task<IEnumerable<(string Schema, string Name, string Type)?>> GetSqlModulesAsync(Guid connectionId)
    {
        var connectionString = await GetSqlConnectionStringAsync(connectionId);
        ArgumentNullException.ThrowIfNull(connectionString);

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
        var connectionString = await GetSqlConnectionStringAsync(connectionId);
        ArgumentNullException.ThrowIfNull(connectionString);
        using var sqlConnection = new SqlConnection(connectionString);
        var definition = await sqlConnection.ExecuteScalarAsync<string>(
            "SELECT OBJECT_DEFINITION(OBJECT_ID(@ObjectName))",
            new { ObjectName = objectName});
        return definition;
    }

    public async Task<IEnumerable<SqlReference>> GetSqlReferencedObjectsAsync(
        Guid connectionId,
        string referencingSchemaOperator = "like",
        string referencingSchemaFilter = "",
        string referencingNameOperator = "like",
        string referencingNameFilter = "",
        string referencedSchemaOperator = "like",
        string referencedSchemaFilter = "",
        string referencedNameOperator = "like",
        string referencedNameFilter = "")
    {
        var allowedOperators = new[] { "=", "like" };
        if (!allowedOperators.Contains(referencingSchemaOperator))
            throw new ArgumentException($"Invalid operator {referencingSchemaOperator}", nameof(referencingSchemaOperator));
        if (!allowedOperators.Contains(referencingNameOperator))
            throw new ArgumentException($"Invalid operator {referencingNameOperator}", nameof(referencingNameOperator));
        if (!allowedOperators.Contains(referencedSchemaOperator))
            throw new ArgumentException($"Invalid operator {referencedSchemaOperator}", nameof(referencedSchemaOperator));
        if (!allowedOperators.Contains(referencedNameOperator))
            throw new ArgumentException($"Invalid operator {referencedNameOperator}", nameof(referencedNameOperator));

        if (!referencingSchemaFilter.Any()) referencingSchemaOperator = "like";
        if (!referencingNameFilter.Any()) referencingNameOperator = "like";
        if (!referencedSchemaFilter.Any()) referencedSchemaOperator = "like";
        if (!referencedNameFilter.Any()) referencedNameOperator = "like";

        static string encodeForLike(string term) => term.Replace("[", "[[]").Replace("%", "[%]");
        var encodedReferencingSchemaFilter = referencingSchemaOperator == "=" ? referencingSchemaFilter : $"%{encodeForLike(referencingSchemaFilter)}%";
        var encodedReferencingNameFilter = referencingNameOperator == "=" ? referencingNameFilter : $"%{encodeForLike(referencingNameFilter)}%";
        var encodedReferencedSchemaFilter = referencedSchemaOperator == "=" ? referencedSchemaFilter : $"%{encodeForLike(referencedSchemaFilter)}%";
        var encodedReferencedNameFilter = referencedNameOperator == "=" ? referencedNameFilter :  $"%{encodeForLike(referencedNameFilter)}%";

        var connectionString = await GetSqlConnectionStringAsync(connectionId);
        ArgumentNullException.ThrowIfNull(connectionString);
        using var sqlConnection = new SqlConnection(connectionString);
        var rows = await sqlConnection.QueryAsync<SqlReference>(
            $@"select distinct
	            ReferencingSchema = c.name,
	            ReferencingName = b.name,
	            ReferencingType = b.type_desc,
	            ReferencedDatabase = a.referenced_database_name,
	            ReferencedSchema = isnull(e.name, a.referenced_schema_name),
	            ReferencedName = isnull(d.name, a.referenced_entity_name),
	            ReferencedType = isnull(d.type_desc, 'UNKNOWN')
            from sys.sql_expression_dependencies as a
	            inner join sys.objects as b on a.referencing_id = b.object_id
	            inner join sys.schemas as c on b.schema_id = c.schema_id
	            left join sys.objects as d on a.referenced_id = d.object_id
	            left join sys.schemas as e on d.schema_id = e.schema_id
            where
                c.name {referencingSchemaOperator} @ReferencingSchemaFilter and
                b.name {referencingNameOperator} @ReferencingNameFilter and
                isnull(e.name, a.referenced_schema_name) {referencedSchemaOperator} @ReferencedSchemaFilter and
                isnull(d.name, a.referenced_entity_name) {referencedNameOperator} @ReferencedNameFilter
            order by
	            ReferencingSchema,
	            ReferencingName,
	            ReferencedDatabase,
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

    public async Task<IEnumerable<(string ServerName, string DatabaseName, string SchemaName, string ObjectName, bool IsUnreliable)>> GetSourceObjectsAsync(Guid connectionId, string? schema, string name)
    {
        var connectionString = await GetSqlConnectionStringAsync(connectionId);
        ArgumentNullException.ThrowIfNull(connectionString);
        using var sqlConnection = new SqlConnection(connectionString);

        schema ??= "[dbo]";
        var objectName = $"{schema}.[{name}]";
        var rows = await sqlConnection.QueryAsync<(string, string, string, string, bool)>(
            @"select distinct
                referenced_server_name = isnull(a.referenced_server_name, @@servername),
                referenced_database_name = isnull(a.referenced_database_name, db_name()),
                referenced_schema_name = isnull(a.referenced_schema_name, c.name),
                referenced_entity_name = a.referenced_entity_name,
                is_unreliable = case when a.is_select_all = 0 and a.is_selected = 0 then 1 else 0 end
            from sys.dm_sql_referenced_entities(@ObjectName, 'OBJECT') as a
                left join sys.objects as b on a.referenced_id = b.object_id and a.referenced_entity_name = b.name
                left join sys.schemas as c on b.schema_id = c.schema_id
            where is_updated = 0
                and isnull(a.referenced_server_name, @@servername) is not null
                and isnull(a.referenced_database_name, db_name()) is not null
                and isnull(a.referenced_schema_name, c.name) is not null
                and a.referenced_entity_name is not null
                and isnull(b.[type], 'U') in ('U','V')", new
            {
                ObjectName = objectName
            });
        return rows;
    }

    public async Task<IEnumerable<(string ServerName, string DatabaseName, string SchemaName, string ObjectName, bool IsUnreliable)>> GetTargetObjectsAsync(Guid connectionId, string? schema, string name)
    {
        var connectionString = await GetSqlConnectionStringAsync(connectionId);
        ArgumentNullException.ThrowIfNull(connectionString);
        using var sqlConnection = new SqlConnection(connectionString);

        schema ??= "[dbo]";
        var objectName = $"{schema}.[{name}]";
        var rows = await sqlConnection.QueryAsync<(string, string, string, string, bool)>(
            @"select distinct
                referenced_server_name = isnull(a.referenced_server_name, @@servername),
                referenced_database_name = isnull(a.referenced_database_name, db_name()),
                referenced_schema_name = isnull(a.referenced_schema_name, c.name),
                referenced_entity_name = a.referenced_entity_name,
                is_unreliable = case when a.is_updated = 0 then 1 else 0 end
            from sys.dm_sql_referenced_entities(@ObjectName, 'OBJECT') as a
                left join sys.objects as b on a.referenced_id = b.object_id and a.referenced_entity_name = b.name
                left join sys.schemas as c on b.schema_id = c.schema_id
            where is_selected = 0
                and is_select_all = 0
                and isnull(a.referenced_server_name, @@servername) is not null
                and isnull(a.referenced_database_name, db_name()) is not null
                and isnull(a.referenced_schema_name, c.name) is not null
                and a.referenced_entity_name is not null
                and isnull(b.[type], 'U') in ('U','V')", new
            {
                ObjectName = objectName
            });
        return rows;
    }

    public async Task<IEnumerable<(string Server, string Database, string Schema, string Object, string Type)>> GetDatabaseObjectsAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connectionString = await GetSqlConnectionStringAsync(connectionId, cancellationToken);
        ArgumentNullException.ThrowIfNull(connectionString);
        using var sqlConnection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            @"select
                [server_name] = @@servername,
                [database_name] = db_name(),
                [schema_name] = b.name,
                [object_name] = a.name,
                [object_type] = a.type_desc
            from sys.objects as a
                join sys.schemas as b on a.schema_id = b.schema_id
            where a.[type] in ('U', 'V')
            order by b.name, a.name", cancellationToken: cancellationToken);
        var rows = await sqlConnection.QueryAsync<(string, string, string, string, string)>(command);
        return rows;
    }

    public async Task<IEnumerable<(string Schema, string Table, bool HasPrimaryKey)>> GetDatabaseTablesAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connectionString = await GetSqlConnectionStringAsync(connectionId, cancellationToken);
        ArgumentNullException.ThrowIfNull(connectionString);
        using var sqlConnection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            @"select
                [schema_name] = b.[name],
                [table_name] = a.[name],
                [has_pk] = convert(bit, case when c.[index_id] is not null then 1 else 0 end)
            from [sys].[tables] as a
                inner join [sys].[schemas] as b on a.[schema_id] = b.[schema_id]
                left join [sys].[indexes] as c on a.[object_id] = c.[object_id] and c.[is_primary_key] = 1
            order by [schema_name], [table_name]",
            cancellationToken: cancellationToken);
        var rows = await sqlConnection.QueryAsync<(string, string, bool)>(command);
        return rows;
    }

    public async Task<List<AsModel>> GetAnalysisServicesModelsAsync(Guid connectionId)
    {
        var connectionString = await GetAsConnectionStringAsync(connectionId);
        ArgumentNullException.ThrowIfNull(connectionString);

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

    private async Task<string?> GetSqlConnectionStringAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var connectionString = await context.SqlConnections
            .AsNoTracking()
            .Where(c => c.ConnectionId == connectionId)
            .Select(c => c.ConnectionString)
            .FirstOrDefaultAsync(cancellationToken);
        return connectionString;
    }

    private async Task<string?> GetAsConnectionStringAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        using var context = _dbContextFactory.CreateDbContext();
        var connectionString = await context.AnalysisServicesConnections
            .AsNoTracking()
            .Where(c => c.ConnectionId == connectionId)
            .Select(c => c.ConnectionString)
            .FirstOrDefaultAsync(cancellationToken);
        return connectionString;
    }

}
