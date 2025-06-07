using Biflow.Core.Entities;
using Dapper;
using Snowflake.Data.Client;

namespace Biflow.Ui.SqlMetadataExtensions;

public static class SnowflakeExtensions
{
    public static async Task<IEnumerable<SnowflakeStoredProcedure>> GetStoredProceduresAsync(this SnowflakeConnection connection)
    {
        await using var sfConnection = new SnowflakeDbConnection(connection.ConnectionString);
        const string sql = """
            select
                p.PROCEDURE_SCHEMA as "SchemaName",
                p.PROCEDURE_NAME as "ProcedureName",
                p.ARGUMENT_SIGNATURE as "ArgumentSignature",
                split_part(trim(t.VALUE), ' ', 1) as "ParameterName",
                split_part(trim(t.VALUE), ' ', 2) as "ParameterType"
            from INFORMATION_SCHEMA.PROCEDURES p,
                table(split_to_table(substring(p.ARGUMENT_SIGNATURE, 2, length(p.ARGUMENT_SIGNATURE) - 2), ',')) t
            """;
        var procs = new HashSet<SnowflakeStoredProcedure>();
        _ = await sfConnection.QueryAsync<SnowflakeStoredProcedure, SnowflakeStoredProcedureParameter?, SnowflakeStoredProcedure>(
            sql,
            (proc, param) =>
            {
                if (!procs.TryGetValue(proc, out var storedProc))
                {
                    storedProc = proc;
                    procs.Add(storedProc);
                }
                if (param is not null 
                && !string.IsNullOrWhiteSpace(param.ParameterName) 
                && !string.IsNullOrWhiteSpace(param.ParameterType))
                {
                    storedProc.Parameters.Add(param);
                }
                return storedProc;
            },
            splitOn: "ParameterName");
        return procs;
    }

    public static async Task<string?> GetProcedureDefinitionAsync(this SnowflakeConnection connection, SnowflakeStoredProcedure procedure)
    {
        await using var sfConnection = new SnowflakeDbConnection(connection.ConnectionString);
        const string sql = """
            select PROCEDURE_DEFINITION
            from INFORMATION_SCHEMA.PROCEDURES
            where PROCEDURE_SCHEMA = :schema and PROCEDURE_NAME = :name and ARGUMENT_SIGNATURE = :arguments
            limit 1
            """;
        var definition = await sfConnection.ExecuteScalarAsync<string?>(
            sql,
            param: new
            {
                schema = procedure.SchemaName,
                name = procedure.ProcedureName,
                arguments = procedure.ArgumentSignature
            });
        return definition;
    }

    public static async Task<IEnumerable<DbObject>> GetDatabaseObjectsAsync(
        this SnowflakeConnection connection,
        string? schemaNameSearchTerm = null,
        string? objectNameSearchTerm = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        await using var sfConnection = new SnowflakeDbConnection(connection.ConnectionString);
        var limitTerm = limit > 0 ? $"limit {limit}" : "";
        var schema = string.IsNullOrEmpty(schemaNameSearchTerm) ? null : $"%{schemaNameSearchTerm.ToLower()}%";
        var name = string.IsNullOrEmpty(objectNameSearchTerm) ? null : $"%{objectNameSearchTerm.ToLower()}%";
        var command = new CommandDefinition($"""
            select
                CURRENT_ACCOUNT_NAME() as SERVER_NAME,
                TABLE_CATALOG as DATABASE_NAME,
                TABLE_SCHEMA as SCHEMA_NAME,
                TABLE_NAME as OBJECT_NAME,
                TABLE_TYPE as OBJECT_TYPE
            from INFORMATION_SCHEMA.TABLES
            where (
                    :schema is null or lower(TABLE_SCHEMA) like :schema
                ) and (
                    :name is null or lower(TABLE_NAME) like :name
                )
            order by SCHEMA_NAME, OBJECT_NAME
            {limitTerm}
            """, new { schema, name }, cancellationToken: cancellationToken);
        var rows = await sfConnection.QueryAsync<(string, string, string, string, string)>(command);
        return rows
            .Select(r => new DbObject(r.Item1, r.Item2, r.Item3, r.Item4, r.Item5))
            .ToArray();
    }
}