﻿using Biflow.Core.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;
using Microsoft.AnalysisServices.Tabular;

namespace Biflow.Ui.SqlMetadataExtensions;

public static partial class MsSqlExtensions
{
    public static async Task<SSISCatalog> GetCatalogPackagesAsync(this MsSqlConnection connection)
    {
        await using var sqlConnection = new SqlConnection(connection.ConnectionString);
        var folders = new Dictionary<long, CatalogFolder>();
        await connection.RunImpersonatedOrAsCurrentUserAsync(() =>
            sqlConnection.QueryAsync<CatalogFolder, CatalogProjectDto?, CatalogPackageDto?, CatalogParameter?, CatalogFolder>("""
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

                    if (project is null)
                    {
                        return folderEntry;
                    }
                    
                    if (!folderEntry.Projects.TryGetValue(project.ProjectId, out var projectEntry))
                    {
                        projectEntry = new(project.ProjectId, project.ProjectName, folderEntry);
                        folderEntry.Projects[projectEntry.ProjectId] = projectEntry;
                    }

                    if (package is null)
                    {
                        return folderEntry;
                    }
                    
                    if (!projectEntry.Packages.TryGetValue(package.PackageId, out var packageEntry))
                    {
                        packageEntry = new(package.PackageId, package.PackageName, projectEntry);
                        projectEntry.Packages[packageEntry.PackageId] = packageEntry;
                    }

                    if (param is null)
                    {
                        return folderEntry;
                    }

                    if (packageEntry.Parameters.TryGetValue(param.ParameterId, out var paramEntry))
                    {
                        return folderEntry;
                    }
                    
                    paramEntry = param;
                    packageEntry.Parameters[paramEntry.ParameterId] = paramEntry;
                    
                    return folderEntry;
                },
                splitOn: "ProjectId,PackageId,ParameterId"));
        return new SSISCatalog(folders);
    }

    public static async Task<IEnumerable<PackageParameter>> GetPackageParametersAsync(
        this MsSqlConnection connection,
        string folder,
        string project,
        string package,
        bool includeConnectionManagerParameters)
    {
        await using var sqlConnection = new SqlConnection(connection.ConnectionString);
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

    public static async Task<IEnumerable<MsSqlStoredProcedure>> GetStoredProceduresAsync(this MsSqlConnection connection)
    {
        await using var sqlConnection = new SqlConnection(connection.ConnectionString);
        const string sql = """
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
        var procedures = new Dictionary<int, MsSqlStoredProcedure>();
        await connection.RunImpersonatedOrAsCurrentUserAsync(() =>
            sqlConnection.QueryAsync<MsSqlStoredProcedure, MsSqlStoredProcedureParameter?, MsSqlStoredProcedure>(
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

    public static async Task<MsSqlStoredProcedure?> GetStoredProcedureAsync(this MsSqlConnection connection, string? schema, string name)
    {
        await using var sqlConnection = new SqlConnection(connection.ConnectionString);
        const string sql = """
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
           where b.name = @schema and a.name = @name
           order by
               ParameterId
           """;
        MsSqlStoredProcedure? procedure = null;
        schema ??= "dbo";
        await connection.RunImpersonatedOrAsCurrentUserAsync(() =>
            sqlConnection.QueryAsync<MsSqlStoredProcedure, MsSqlStoredProcedureParameter?, MsSqlStoredProcedure>(
                sql,
                (proc, param) =>
                {
                    procedure ??= proc;
                    if (param is not null)
                    {
                        procedure.Parameters.Add(param);
                    }
                    return procedure;
                },
                splitOn: "ParameterId",
                param: new
                {
                    schema, name
                }));
        return procedure;
    }

    public static async Task<IEnumerable<(string ParameterName, ParameterValue Value)>> GetStoredProcedureParametersAsync(
        this MsSqlConnection connection,
        string schema,
        string procedure)
    {
        await using var sqlConnection = new SqlConnection(connection.ConnectionString);
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
                    { } a when a.Contains("char") => ParameterValueType.String,
                    "tinyint" or "smallint" => ParameterValueType.Int16,
                    "int" => ParameterValueType.Int32,
                    "bigint" => ParameterValueType.Int64,
                    "smallmoney" or "money" or "numeric" or "decimal" => ParameterValueType.Decimal,
                    "real" => ParameterValueType.Single,
                    "float" => ParameterValueType.Double,
                    { } d when d.Contains("date") => ParameterValueType.DateTime,
                    "bit" => ParameterValueType.Boolean,
                    _ => ParameterValueType.String
                };
                var value = ParameterValue.DefaultValue(type);
                return (param.Name, value);
            }).ToArray();
        });
    }

    public static async Task<IEnumerable<(string AgentJobName, bool IsEnabled)>> GetAgentJobsAsync(this MsSqlConnection connection)
    {
        await using var sqlConnection = new SqlConnection(connection.ConnectionString);
        var rows = await connection.RunImpersonatedOrAsCurrentUserAsync(
            () => sqlConnection.QueryAsync<dynamic>("EXEC msdb.dbo.sp_help_job"));
        var agentJobs = rows.Select(r => ((string)r.name, ((short)r.enabled) > 0)).ToArray();
        return agentJobs;
    }

    public static async Task<string?> GetProcedureDefinitionAsync(this MsSqlConnection connection, MsSqlStoredProcedure procedure)
    {
        await using var sqlConnection = new SqlConnection(connection.ConnectionString);
        var definition = await connection.RunImpersonatedOrAsCurrentUserAsync(
            () => sqlConnection.ExecuteScalarAsync<string>(
                "SELECT OBJECT_DEFINITION(@ObjectId)",
                new { ObjectId = procedure.ProcedureId}));
        return definition;
    }

    public static async Task<IEnumerable<SqlReference>> GetSqlReferencedObjectsAsync(
        this MsSqlConnection connection,
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

        await using var sqlConnection = new SqlConnection(connection.ConnectionString);
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

    public static async Task<IEnumerable<DbObjectReference>> GetSourceObjectsAsync(
        this MsSqlConnection connection,
        string? schema,
        string name)
    {
        await using var sqlConnection = new SqlConnection(connection.ConnectionString);

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
            .Select(r => new DbObjectReference
            {
                ServerName = r.Item1,
                DatabaseName = r.Item2,
                SchemaName = r.Item3,
                ObjectName = r.Item4,
                IsUnreliable = r.Item5
            })
            .ToArray();
    }

    public static async Task<IEnumerable<DbObjectReference>> GetTargetObjectsAsync(
        this MsSqlConnection connection,
        string? schema,
        string name)
    {
        await using var sqlConnection = new SqlConnection(connection.ConnectionString);

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
            .Select(r => new DbObjectReference
            {
                ServerName = r.Item1,
                DatabaseName = r.Item2,
                SchemaName = r.Item3,
                ObjectName = r.Item4,
                IsUnreliable = r.Item5
            })
            .ToArray();
    }

    public static async Task<IEnumerable<DbObject>> GetDatabaseObjectsAsync(
        this MsSqlConnection connection,
        string? schemaNameSearchTerm = null,
        string? objectNameSearchTerm = null,
        int? top = null,
        CancellationToken cancellationToken = default)
    {
        await using var sqlConnection = new SqlConnection(connection.ConnectionString);
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

    public static async Task<IEnumerable<DbTable>> GetDatabaseTablesAsync(this MsSqlConnection connection, CancellationToken cancellationToken = default)
    {
        await using var sqlConnection = new SqlConnection(connection.ConnectionString);
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

    public static async Task<AsServer> GetAnalysisServicesModelsAsync(this AnalysisServicesConnection connection)
    {
        return await connection.RunImpersonatedOrAsCurrentUserAsync(() =>
        {
            return Task.Run(() =>
            {
                using var server = new Server();
                server.Connect(connection.ConnectionString);
                var models = new List<AsModel>();
                var asServer = new AsServer(server.Name, models);
                var databases = server.Databases;
                for (var dbi = 0; dbi < databases.Count; dbi++)
                {
                    var database = databases[dbi];
                    var model = database.Model;
                    var tables = new List<AsTable>();
                    var asModel = new AsModel(database.Name, tables, asServer);
                    foreach (var table in model.Tables)
                    {
                        var partitions = new List<AsPartition>();
                        var asTable = new AsTable(table.Name, asModel, partitions);
                        partitions.AddRange(
                            table.Partitions.Select(partition => new AsPartition(partition.Name, asTable)));
                        tables.Add(asTable);
                    }
                    models.Add(asModel);
                }
                return asServer;
            });
        });
    }

    /// <summary>
    /// Try to identify and parse a SQL stored procedure from a SQL statement
    /// </summary>
    /// <remarks>For example, SQL statement <c>exec [dbo].[MyProc]</c> would return a schema of dbo and procedure name MyProc</remarks>
    /// <returns>Tuple of strings if the stored procedure was parsed successfully, null if not. The schema is null if the SQL statement did not include a schema.</returns>
    public static (string? Schema, string ProcedureName)? ParseStoredProcedureFromSqlStatement(string sqlStatement)
    {
        // Can handle white space inside object names
        var match1 = ProcedureWithSchemaWithBracketsRegex.Match(sqlStatement);
        if (match1.Success)
        {
            var schema = match1.Groups[1].Value[1..^1]; // skip first and last character
            var proc = match1.Groups[2].Value[1..^1];
            return (schema, proc);
        }

        // No square brackets => no whitespace in object names
        var match2 = ProcedureWithSchemaWithoutBracketsRegex.Match(sqlStatement);
        if (match2.Success)
        {
            var schema = match2.Groups[1].Value;
            var proc = match2.Groups[2].Value;
            return (schema, proc);
        }

        // Can handle white space inside object names
        var match3 = ProcedureWithoutSchemaWithBracketsRegex.Match(sqlStatement);
        if (match3.Success)
        {
            var proc = match3.Groups[1].Value[1..^1]; // skip first and last character
            return (null, proc);
        }

        // No square brackets => no whitespace in object names
        var match4 = ProcedureWithoutSchemaWithoutBracketsRegex.Match(sqlStatement);
        if (match4.Success)
        {
            var proc = match4.Groups[1].Value;
            return (null, proc);
        }

        return null;
    }

    private static string EncodeForLike(this string term) => term.Replace("[", "[[]").Replace("%", "[%]");

    // Using the GeneratedRegex attributes we can create the regex already at compile time.

    // Can handle white space inside object names
    [GeneratedRegex(@"EXEC(?:UTE)?[\s*](\[.*\]).(\[.*\])", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ProcedureWithSchemaWithBracketsRegex { get; }

    // No square brackets => no whitespace in object names
    [GeneratedRegex(@"EXEC(?:UTE)?[\s*](\S*)\.(\S*)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ProcedureWithSchemaWithoutBracketsRegex { get; }

    // Can handle white space inside object names
    [GeneratedRegex(@"EXEC(?:UTE)?[\s*](\[.*\])", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ProcedureWithoutSchemaWithBracketsRegex { get; }

    // No square brackets => no whitespace in object names
    [GeneratedRegex(@"EXEC(?:UTE)?[\s*](\S*)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ProcedureWithoutSchemaWithoutBracketsRegex { get; }
}