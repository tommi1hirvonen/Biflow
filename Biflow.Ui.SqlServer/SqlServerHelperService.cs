using Biflow.Core;
using Biflow.Core.Entities;
using Biflow.DataAccess;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.SqlServer;

public class SqlServerHelperService(IDbContextFactory<AppDbContext> dbContextFactory)
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory = dbContextFactory;

    public async Task<SSISCatalog> GetCatalogPackagesAsync(Guid connectionId)
    {
        var connection = await GetSqlConnectionAsync(connectionId);
        using var sqlConnection = new SqlConnection(connection.ConnectionString);
        var folders = new Dictionary<long, CatalogFolder>();
        var rows = await connection.RunImpersonatedOrAsCurrentUserAsync(
            () => sqlConnection.QueryAsync<CatalogFolder, CatalogProjectDto?, CatalogPackageDto?, CatalogParameter?, CatalogFolder>("""
                SELECT
                    FolderId = [f].[folder_id],
                    FolderName = [f].[name],
                    ProjectId = [pr].[project_id],
                    ProjectName = [pr].[name],
                    PackageId = [pa].[package_id],
                    PackageName = [pa].[name],
                    ParameterId = [pap].[parameter_id],
                    ParameterName = [pap].[parameter_name],
                    ParameterType = [pap].[data_type],
                    DesignDefaultValue = [pap].[design_default_value],
                    DefaultValue = [pap].[default_value],
                    ConnectionManagerParameter = CASE WHEN [pap].[parameter_name] LIKE 'CM.%' THEN 1 ELSE 0 END,
                    ProjectParameter = 0
                FROM [SSISDB].[catalog].[folders] AS [f]
                    LEFT JOIN [SSISDB].[catalog].[projects] AS [pr] ON [f].[folder_id] = [pr].[folder_id]
                    LEFT JOIN [SSISDB].[catalog].[packages] AS [pa] ON [pr].[project_id] = [pa].[project_id]
                    LEFT JOIN [SSISDB].[catalog].[object_parameters] AS [pap] ON
                        [pa].[project_id] = [pap].[project_id] AND
                        [pa].[name] = [pap].[object_name] AND
                        [pap].[object_type] = 30
                UNION ALL
                SELECT
                    FolderId = [f].[folder_id],
                    FolderName = [f].[name],
                    ProjectId = [pr].[project_id],
                    ProjectName = [pr].[name],
                    PackageId = [pa].[package_id],
                    PackageName = [pa].[name],
                    ParameterId = [prp].[parameter_id],
                    ParameterName = [prp].[parameter_name],
                    ParameterType = [prp].[data_type],
                    DesignDefaultValue = [prp].[design_default_value],
                    DefaultValue = [prp].[default_value],
                    ConnectionManagerParameter = CASE WHEN [prp].[parameter_name] LIKE 'CM.%' THEN 1 ELSE 0 END,
                    ProjectParameter = 1
                FROM [SSISDB].[catalog].[folders] AS [f]
                    INNER JOIN [SSISDB].[catalog].[projects] AS [pr] ON [f].[folder_id] = [pr].[folder_id]
                    INNER JOIN [SSISDB].[catalog].[packages] AS [pa] ON [pr].[project_id] = [pa].[project_id]
                    INNER JOIN [SSISDB].[catalog].[object_parameters] AS [prp] ON
                        [pa].[project_id] = [prp].[project_id] AND
                        [pr].[name] = [prp].[object_name] AND
                        [prp].[object_type] = 20
                """,
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
                            projectEntry = new(project.ProjectId, project.ProjectName, folderEntry);
                            folderEntry.Projects[projectEntry.ProjectId] = projectEntry;
                        }
                        if (package is not null)
                        {
                            if (!projectEntry.Packages.TryGetValue(package.PackageId, out var packageEntry))
                            {
                                packageEntry = new(package.PackageId, package.PackageName, projectEntry);
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
                splitOn: "ProjectId,PackageId,ParameterId"));
        return new SSISCatalog(folders);
    }

    public async Task<IEnumerable<PackageParameter>> GetPackageParametersAsync(
        Guid connectionId, string folder, string project, string package, bool includeConnectionManagerParameters)
    {
        var connection = await GetSqlConnectionAsync(connectionId);
        using var sqlConnection = new SqlConnection(connection.ConnectionString);
        return await connection.RunImpersonatedOrAsCurrentUserAsync(async () =>
        {
            var count = await sqlConnection.ExecuteScalarAsync<int>("""
                SELECT COUNT(*)
                FROM [SSISDB].[catalog].[folders]
                    INNER JOIN [SSISDB].[catalog].[projects] ON [folders].[folder_id] = [projects].[folder_id]
                    INNER JOIN [SSISDB].[catalog].[packages] ON [projects].[project_id] = [packages].[project_id]
                WHERE [folders].[name] = @FolderName AND [projects].[name] = @ProjectName AND [packages].[name] = @PackageName
                """, new
                {
                    FolderName = folder,
                    ProjectName = project,
                    PackageName = package
                });

            if (count == 0)
            {
                throw new ObjectNotFoundException($"{folder}/{project}/{package}");
            }

            var rows = await sqlConnection.QueryAsync<(string Level, string Name, string Type, object? Default)>("""
                SELECT
                    ParameterLevel = 'Project',
                    ParameterName = [object_parameters].[parameter_name],
                    ParameterType = [object_parameters].[data_type],
                    DefaultValue = [object_parameters].[design_default_value]
                FROM [SSISDB].[catalog].[folders]
                    INNER JOIN [SSISDB].[catalog].[projects] ON [folders].[folder_id] = [projects].[folder_id]
                    INNER JOIN [SSISDB].[catalog].[object_parameters] ON
                        [projects].[project_id] = [object_parameters].[project_id] AND
                        [projects].[name] = [object_parameters].[object_name] AND
                        [object_parameters].[object_type] = 20
                WHERE [folders].[name] = @FolderName AND
                    [projects].[name] = @ProjectName AND
                    ([object_parameters].[parameter_name] NOT LIKE 'CM.%' OR @IncludeCMParams = 1)
                UNION ALL
                SELECT
                    ParameterLevel = 'Package',
                    ParameterName = [object_parameters].[parameter_name],
                    ParameterType = [object_parameters].[data_type],
                    DefaultValue = [object_parameters].[design_default_value]
                FROM [SSISDB].[catalog].[folders]
                    INNER JOIN [SSISDB].[catalog].[projects] ON [folders].[folder_id] = [projects].[folder_id]
                    INNER JOIN [SSISDB].[catalog].[packages] ON [projects].[project_id] = [packages].[project_id]
                    INNER JOIN [SSISDB].[catalog].[object_parameters] ON
                        [packages].[project_id] = [object_parameters].[project_id] AND
                        [packages].[name] = [object_parameters].[object_name] AND
                        [object_parameters].[object_type] = 30
                WHERE [folders].[name] = @FolderName AND
                    [projects].[name] = @ProjectName AND
                    [packages].[name] = @PackageName AND
                    ([object_parameters].[parameter_name] NOT LIKE 'CM.%' OR @IncludeCMParams = 1)
                """, new
                {
                    FolderName = folder,
                    ProjectName = project,
                    PackageName = package,
                    IncludeCMParams = includeConnectionManagerParameters
                });
            return rows.Select(param =>
            {
                var level = Enum.Parse<ParameterLevel>(param.Level);
                if (!Enum.TryParse(param.Type, out ParameterValueType type))
                {
                    type = param.Type switch
                    {
                        "UInt32" => ParameterValueType.Int32,
                        "UInt64" => ParameterValueType.Int64,
                        _ => ParameterValueType.String
                    };
                }
                _ = ParameterValue.TryCreate(type, param.Default, out var value);
                return new PackageParameter(level, param.Name, value);
            }).ToArray();
        });
    }

    public async Task<IEnumerable<StoredProcedure>> GetStoredProceduresAsync(Guid connectionId)
    {
        var connection = await GetSqlConnectionAsync(connectionId);
        using var sqlConnection = new SqlConnection(connection.ConnectionString);
        var sql = """
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
                ParameterId
            """;
        var procedures = new Dictionary<int, StoredProcedure>();
        var data = await connection.RunImpersonatedOrAsCurrentUserAsync(
            () => sqlConnection.QueryAsync<StoredProcedure, StoredProcedureParameter?, StoredProcedure>(
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
                splitOn: "ParameterId"));
        return procedures.Values.ToArray();
    }

    public async Task<IEnumerable<(string ParameterName, ParameterValue Value)>> GetStoredProcedureParametersAsync(Guid connectionId, string schema, string procedure)
    {
        var connection = await GetSqlConnectionAsync(connectionId);
        using var sqlConnection = new SqlConnection(connection.ConnectionString);
        return await connection.RunImpersonatedOrAsCurrentUserAsync(async () =>
        {
            var objectId = await sqlConnection.ExecuteScalarAsync<long?>("""
                select top 1 object_id
                from sys.procedures
                where name = @procedure and object_schema_name(object_id) = @schema
                """,
                new { procedure, schema })
                ?? throw new ObjectNotFoundException($"{schema}.{procedure}");

            var rows = await sqlConnection.QueryAsync<(string Name, string Type)>("""
                select
                    ParameterName = name,
                    ParameterType = TYPE_NAME(user_type_id)
                from sys.parameters
                where object_id = @objectId
                """, param: new
                {
                    objectId
                });

            return rows.Select(param =>
            {
                var type = param.Type switch
                {
                    string a when a.Contains("char") => ParameterValueType.String,
                    "tinyint" or "smallint" => ParameterValueType.Int16,
                    "int" => ParameterValueType.Int32,
                    "bigint" => ParameterValueType.Int64,
                    "smallmoney" or "money" or "numeric" or "decimal" => ParameterValueType.Decimal,
                    "real" => ParameterValueType.Single,
                    "float" => ParameterValueType.Double,
                    string d when d.Contains("date") => ParameterValueType.DateTime,
                    "bit" => ParameterValueType.Boolean,
                    _ => ParameterValueType.String
                };
                var value = ParameterValue.DefaultValue(type);
                return (param.Name, value);
            }).ToArray();
        });
    }

    public async Task<IEnumerable<(string AgentJobName, bool IsEnabled)>> GetAgentJobsAsync(Guid connectionId)
    {
        var connection = await GetSqlConnectionAsync(connectionId);
        using var sqlConnection = new SqlConnection(connection.ConnectionString);
        var rows = await connection.RunImpersonatedOrAsCurrentUserAsync(
            () => sqlConnection.QueryAsync<dynamic>("EXEC msdb.dbo.sp_help_job"));
        var agentJobs = rows.Select(r => ((string)r.name, ((short)r.enabled) > 0)).ToArray();
        return agentJobs;
    }

    public async Task<IEnumerable<(string Schema, string Name, string Type)?>> GetSqlModulesAsync(Guid connectionId)
    {
        var connection = await GetSqlConnectionAsync(connectionId);
        using var sqlConnection = new SqlConnection(connection.ConnectionString);
        var results = await connection.RunImpersonatedOrAsCurrentUserAsync(
            () => sqlConnection.QueryAsync<(string, string, string)?>("""
                select
                    SchemaName = c.name,
                    ObjectName = b.name,
                    ObjectType = b.type_desc
                from sys.sql_modules as a
                join sys.objects as b on a.object_id = b.object_id
                    join sys.schemas as c on b.schema_id = c.schema_id
                order by
                    SchemaName,
                    ObjectName
                """));
        return results;
    }

    public async Task<string?> GetObjectDefinitionAsync(Guid connectionId, string objectName)
    {
        var connection = await GetSqlConnectionAsync(connectionId);
        using var sqlConnection = new SqlConnection(connection.ConnectionString);
        var definition = await connection.RunImpersonatedOrAsCurrentUserAsync(
            () => sqlConnection.ExecuteScalarAsync<string>(
                "SELECT OBJECT_DEFINITION(OBJECT_ID(@ObjectName))",
                new { ObjectName = objectName}));
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

        if (referencingSchemaFilter.Length == 0) referencingSchemaOperator = "like";
        if (referencingNameFilter.Length == 0) referencingNameOperator = "like";
        if (referencedSchemaFilter.Length == 0) referencedSchemaOperator = "like";
        if (referencedNameFilter.Length == 0) referencedNameOperator = "like";

        var encodedReferencingSchemaFilter = referencingSchemaOperator == "=" ? referencingSchemaFilter : $"%{referencingSchemaFilter.EncodeForLike()}%";
        var encodedReferencingNameFilter = referencingNameOperator == "=" ? referencingNameFilter : $"%{referencingNameFilter.EncodeForLike()}%";
        var encodedReferencedSchemaFilter = referencedSchemaOperator == "=" ? referencedSchemaFilter : $"%{referencedSchemaFilter.EncodeForLike()}%";
        var encodedReferencedNameFilter = referencedNameOperator == "=" ? referencedNameFilter :  $"%{referencedNameFilter.EncodeForLike()}%";

        var connection = await GetSqlConnectionAsync(connectionId);
        using var sqlConnection = new SqlConnection(connection.ConnectionString);
        var rows = await connection.RunImpersonatedOrAsCurrentUserAsync(
            () => sqlConnection.QueryAsync<SqlReference>($"""
                select distinct
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
                    ReferencedName
                """, new
                {
                    ReferencingSchemaFilter = encodedReferencingSchemaFilter,
                    ReferencingNameFilter = encodedReferencingNameFilter,
                    ReferencedSchemaFilter = encodedReferencedSchemaFilter,
                    ReferencedNameFilter = encodedReferencedNameFilter
                }));
        return rows;
    }

    public async Task<IEnumerable<DbObjectReference>> GetSourceObjectsAsync(Guid connectionId, string? schema, string name)
    {
        var connection = await GetSqlConnectionAsync(connectionId);
        using var sqlConnection = new SqlConnection(connection.ConnectionString);

        schema ??= "[dbo]";
        var objectName = $"{schema}.[{name}]";
        var rows = await connection.RunImpersonatedOrAsCurrentUserAsync(
            () => sqlConnection.QueryAsync<(string, string, string, string, bool)>("""
                ;with cte as (
                   select
                       referenced_server_name = isnull(a.referenced_server_name, @@servername),
                       referenced_database_name = isnull(a.referenced_database_name, db_name()),
                       referenced_schema_name = isnull(a.referenced_schema_name, c.name),
                       referenced_entity_name = a.referenced_entity_name,
                       is_unreliable = case when a.is_select_all = 0 and a.is_selected = 0 then 1 else 0 end,
                       [type] = isnull(b.[type], 'U')
                   from sys.dm_sql_referenced_entities(@ObjectName, 'OBJECT') as a
                       left join sys.objects as b on a.referenced_id = b.object_id and a.referenced_entity_name = b.name
                       left join sys.schemas as c on b.schema_id = c.schema_id
                   where a.is_updated = 0
                       and isnull(a.referenced_server_name, @@servername) is not null
                       and isnull(a.referenced_database_name, db_name()) is not null
                       and isnull(a.referenced_schema_name, c.name) is not null
                       and a.referenced_entity_name is not null
                       and isnull(b.[type], 'U') in ('U','V')
                   union all
                   select
                       referenced_server_name = isnull(b.referenced_server_name, @@servername),
                       referenced_database_name = isnull(b.referenced_database_name, db_name()),
                       referenced_schema_name = isnull(b.referenced_schema_name, c.name),
                       referenced_entity_name = b.referenced_entity_name,
                       is_unreliable = case when b.is_select_all = 0 and b.is_selected = 0 then 1 else 0 end,
                       [type] = isnull(c.[type], 'U')
                   from cte as a
                       cross apply sys.dm_sql_referenced_entities(a.referenced_schema_name + '.' + a.referenced_entity_name, 'OBJECT') as b
                       inner join sys.objects as c on b.referenced_id = c.object_id and b.referenced_entity_name = c.name
                       inner join sys.schemas as d on c.schema_id = d.schema_id
                   where a.[type] = 'V'
                       and b.is_updated = 0
                       and isnull(b.referenced_server_name, @@servername) is not null
                       and isnull(b.referenced_database_name, db_name()) is not null
                       and isnull(b.referenced_schema_name, c.name) is not null
                       and b.referenced_entity_name is not null
                       and isnull(c.[type], 'U') in ('U','V')

                )
                select distinct
                   referenced_server_name,
                   referenced_database_name,
                   referenced_schema_name,
                   referenced_entity_name,
                   is_unreliable
                from cte
                where [type] = 'U'
                """, new
                {
                    ObjectName = objectName
                }));
        return rows
            .Select(r => new DbObjectReference(r.Item1, r.Item2, r.Item3, r.Item4, r.Item5))
            .ToArray();
    }

    public async Task<IEnumerable<DbObjectReference>> GetTargetObjectsAsync(Guid connectionId, string? schema, string name)
    {
        var connection = await GetSqlConnectionAsync(connectionId);
        using var sqlConnection = new SqlConnection(connection.ConnectionString);

        schema ??= "[dbo]";
        var objectName = $"{schema}.[{name}]";
        var rows = await connection.RunImpersonatedOrAsCurrentUserAsync(
            () => sqlConnection.QueryAsync<(string, string, string, string, bool)>("""
                ;with cte as (
                    select
                        referenced_server_name = isnull(a.referenced_server_name, @@servername),
                        referenced_database_name = isnull(a.referenced_database_name, db_name()),
                        referenced_schema_name = isnull(a.referenced_schema_name, c.name),
                        referenced_entity_name = a.referenced_entity_name,
                        is_unreliable = case when a.is_updated = 0 then 1 else 0 end,
                        [type] = isnull(b.[type], 'U')
                    from sys.dm_sql_referenced_entities(@ObjectName, 'OBJECT') as a
                        left join sys.objects as b on a.referenced_id = b.object_id and a.referenced_entity_name = b.name
                        left join sys.schemas as c on b.schema_id = c.schema_id
                    where a.is_selected = 0
                        and a.is_select_all = 0
                        and isnull(a.referenced_server_name, @@servername) is not null
                        and isnull(a.referenced_database_name, db_name()) is not null
                        and isnull(a.referenced_schema_name, c.name) is not null
                        and a.referenced_entity_name is not null
                        and isnull(b.[type], 'U') in ('U','V')
                    union all
                    select
                        referenced_server_name = isnull(b.referenced_server_name, @@servername),
                        referenced_database_name = isnull(b.referenced_database_name, db_name()),
                        referenced_schema_name = isnull(b.referenced_schema_name, c.name),
                        referenced_entity_name = b.referenced_entity_name,
                        is_unreliable = case when b.is_updated = 0 then 1 else 0 end,
                        [type] = isnull(c.[type], 'U')
                    from cte as a
                        cross apply sys.dm_sql_referenced_entities(a.referenced_schema_name + '.' + a.referenced_entity_name, 'OBJECT') as b
                        inner join sys.objects as c on b.referenced_id = c.object_id and b.referenced_entity_name = c.name
                        inner join sys.schemas as d on c.schema_id = d.schema_id
                    where a.[type] = 'V'
                        and b.is_selected = 0
                        and b.is_select_all = 0
                        and isnull(b.referenced_server_name, @@servername) is not null
                        and isnull(b.referenced_database_name, db_name()) is not null
                        and isnull(b.referenced_schema_name, c.name) is not null
                        and b.referenced_entity_name is not null
                        and isnull(c.[type], 'U') in ('U','V')
                )
                select distinct
                    referenced_server_name,
                    referenced_database_name,
                    referenced_schema_name,
                    referenced_entity_name,
                    is_unreliable
                from cte
                """, new
                {
                    ObjectName = objectName
                }));
        return rows
            .Select(r => new DbObjectReference(r.Item1, r.Item2, r.Item3, r.Item4, r.Item5))
            .ToArray();
    }

    public async Task<IEnumerable<DbObject>> GetDatabaseObjectsAsync(
        Guid connectionId,
        string? schemaNameSearchTerm = null,
        string? objectNameSearchTerm = null,
        int? top = null,
        CancellationToken cancellationToken = default)
    {
        var connection = await GetSqlConnectionAsync(connectionId, cancellationToken);
        using var sqlConnection = new SqlConnection(connection.ConnectionString);
        var topTerm = top > 0 ? $"top {top}" : "";
        var schema = string.IsNullOrEmpty(schemaNameSearchTerm) ? null : $"%{schemaNameSearchTerm.EncodeForLike()}%";
        var name = string.IsNullOrEmpty(objectNameSearchTerm) ? null : $"%{objectNameSearchTerm.EncodeForLike()}%";
        var command = new CommandDefinition($"""
            select {topTerm}
                [server_name] = @@servername,
                [database_name] = db_name(),
                [schema_name] = b.name,
                [object_name] = a.name,
                [object_type] = a.type_desc
            from sys.objects as a
                join sys.schemas as b on a.schema_id = b.schema_id
            where a.[type] in ('U', 'V') and (
                    @schema is null or b.name like @schema
                ) and (
                    @name is null or a.name like @name
                )
            order by b.name, a.name
            """, new { schema, name }, cancellationToken: cancellationToken);
        var rows = await connection.RunImpersonatedOrAsCurrentUserAsync(
            () => sqlConnection.QueryAsync<(string, string, string, string, string)>(command));
        return rows
            .Select(r => new DbObject(r.Item1, r.Item2, r.Item3, r.Item4, r.Item5))
            .ToArray();
    }

    public async Task<IEnumerable<DbTable>> GetDatabaseTablesAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        var connection = await GetSqlConnectionAsync(connectionId, cancellationToken);
        using var sqlConnection = new SqlConnection(connection.ConnectionString);
        var command = new CommandDefinition("""
            select
                [schema_name] = b.[name],
                [table_name] = a.[name],
                [has_pk] = convert(bit, case when c.[index_id] is not null then 1 else 0 end)
            from [sys].[tables] as a
                inner join [sys].[schemas] as b on a.[schema_id] = b.[schema_id]
                left join [sys].[indexes] as c on a.[object_id] = c.[object_id] and c.[is_primary_key] = 1
            order by [schema_name], [table_name]
            """,
            cancellationToken: cancellationToken);
        var rows = await connection.RunImpersonatedOrAsCurrentUserAsync(
            () => sqlConnection.QueryAsync<(string, string, bool)>(command));
        return rows
            .Select(r => new DbTable(r.Item1, r.Item2, r.Item3))
            .ToArray();
    }

    public async Task<AsServer> GetAnalysisServicesModelsAsync(Guid connectionId)
    {
        var connection = await GetAsConnectionAsync(connectionId);
        return await connection.RunImpersonatedOrAsCurrentUserAsync(() =>
        {
            return Task.Run(() =>
            {
                using var server = new Microsoft.AnalysisServices.Tabular.Server();
                server.Connect(connection.ConnectionString);
                var models = new List<AsModel>();
                var asServer = new AsServer(server.Name, models);
                var databases = server.Databases;
                for (int dbi = 0; dbi < databases.Count; dbi++)
                {
                    var database = databases[dbi];
                    var model = database.Model;
                    var tables = new List<AsTable>();
                    var asModel = new AsModel(database.Name, tables, asServer);
                    for (int tbi = 0; tbi < model.Tables.Count; tbi++)
                    {
                        var table = model.Tables[tbi];
                        var partitions = new List<AsPartition>();
                        var asTable = new AsTable(table.Name, asModel, partitions);
                        for (int pi = 0; pi < table.Partitions.Count; pi++)
                        {
                            var partition = table.Partitions[pi];
                            var asPartition = new AsPartition(partition.Name, asTable);
                            partitions.Add(asPartition);
                        }
                        tables.Add(asTable);
                    }
                    models.Add(asModel);
                }
                return asServer;
            });
        });
    }

    private async Task<SqlConnectionInfo> GetSqlConnectionAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        using var context = _dbContextFactory.CreateDbContext();
        return await context.SqlConnections
            .AsNoTracking()
            .Include(c => c.Credential)
            .Where(c => c.ConnectionId == connectionId)
            .FirstAsync(cancellationToken);
    }

    private async Task<AnalysisServicesConnectionInfo> GetAsConnectionAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        using var context = _dbContextFactory.CreateDbContext();
        return await context.AnalysisServicesConnections
            .AsNoTracking()
            .Include(c => c.Credential)
            .Where(c => c.ConnectionId == connectionId)
            .FirstAsync(cancellationToken);
    }
}