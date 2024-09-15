using Biflow.Core.Entities;
using Dapper;
using Snowflake.Data.Client;

namespace Biflow.Ui.SqlMetadataExtensions;

public static class SnowflakeExtensions
{
    public static async Task<IEnumerable<SnowflakeStoredProcedure>> GetStoredProceduresAsync(this SnowflakeConnection connection)
    {
        using var sfConnection = new SnowflakeDbConnection(connection.ConnectionString);
        var sql = """
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
        var data = await sfConnection.QueryAsync<SnowflakeStoredProcedure, SnowflakeStoredProcedureParameter?, SnowflakeStoredProcedure>(
            sql,
            (proc, param) =>
            {
                if (!procs.TryGetValue(proc, out var storedProc))
                {
                    storedProc = proc;
                    procs.Add(storedProc);
                }
                if (param is not null)
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
        using var sfConnection = new SnowflakeDbConnection(connection.ConnectionString);
        var sql = """
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
}