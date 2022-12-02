using Biflow.DataAccess.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Biflow.Ui.Core;

public class Upload
{
    private readonly List<Column> _columns;
    private readonly MasterDataTable _table;

    public DataTable Data { get; }

    internal Upload(DataAccess.Models.MasterDataTable table, List<Column> columns, DataTable data)
    {
        _table = table;
        _columns = columns;
        Data = data;
    }

    private IEnumerable<Column> UploadedColumns => _columns.Where(c => !c.IsComputed);

    private IEnumerable<string> UpdateColumns => _columns.Where(c => !c.IsComputed && !c.IsPrimaryKey && !c.IsIdentity).Select(c => c.Name);

    private IEnumerable<string> InsertColumns => _columns.Where(c => !c.IsIdentity && !c.IsComputed).Select(c => c.Name);

    private IEnumerable<string> PkColumns => _columns.Where(c => c.IsPrimaryKey).Select(c => c.Name);

    private string UpdateCommand =>  $"""
        UPDATE target
        SET {string.Join(",\n", UpdateColumns.Select(c => $"[{c}] = src.[{c}]"))}
        FROM [{_table.TargetSchemaName}].[{_table.TargetTableName}] AS target
            INNER JOIN #temp AS src ON {string.Join(" AND ", PkColumns.Select(c => $"target.[{c}] = src.[{c}]"))}
        WHERE NOT EXISTS (
            SELECT {string.Join(',', UpdateColumns.Select(c => $"target.[{c}]"))}
            INTERSECT
            SELECT {string.Join(',', UpdateColumns.Select(c => $"src.[{c}]"))}
        )
        """;

    private string InsertCommand => $"""
        INSERT INTO [{_table.TargetSchemaName}].[{_table.TargetTableName}] (
        {string.Join(",\n", InsertColumns.Select(c => $"[{c}]"))}
        )
        SELECT {string.Join(",\n", InsertColumns.Select(c => $"src.[{c}]"))}
        FROM #temp AS src
            LEFT JOIN [{_table.TargetSchemaName}].[{_table.TargetTableName}] AS target ON
                {string.Join(" AND ", PkColumns.Select(c => $"target.[{c}] = src.[{c}]"))}
        WHERE {string.Join(" AND ", PkColumns.Select(c => $"target.[{c}] IS NULL"))}
        """;

    private string DeleteCommand => $"""
        DELETE FROM target
        FROM [{_table.TargetSchemaName}].[{_table.TargetTableName}] AS target
            LEFT JOIN #temp AS src ON {string.Join(" AND ", PkColumns.Select(c => $"target.[{c}] = src.[{c}]"))}
        WHERE {string.Join(" AND ", PkColumns.Select(c => $"src.[{c}] IS NULL"))}
        """;

    public async Task SaveUploadToDbAsync(UploadType uploadType)
    {
        using var connection = new SqlConnection(_table.Connection.ConnectionString);
        await connection.OpenAsync();

        var columnDefinitions = UploadedColumns.Select(c => $"[{c.Name}] {c.DbCreateDatatype}");
        var command = $"""
            CREATE TABLE #temp (
            {string.Join(",\n", columnDefinitions)}
            )
            """;
        await connection.ExecuteAsync(command);
        
        var copy = new SqlBulkCopy(connection)
        {
            DestinationTableName = "#temp"
        };
        foreach (var column in UploadedColumns)
        {
            var mapping = new SqlBulkCopyColumnMapping(column.Name, column.Name);
            copy.ColumnMappings.Add(mapping);
        }
        await copy.WriteToServerAsync(Data);

        var transaction = await connection.BeginTransactionAsync();
        try
        {
            if (uploadType == UploadType.Upsert)
            {
                await connection.ExecuteAsync(UpdateCommand, transaction: transaction);
                await connection.ExecuteAsync(InsertCommand, transaction: transaction);
            }
            else if (uploadType == UploadType.InsertNew)
            {
                await connection.ExecuteAsync(InsertCommand, transaction: transaction);
            }
            else if (uploadType == UploadType.UpdateExisting)
            {
                await connection.ExecuteAsync(UpdateCommand, transaction: transaction);
            }
            else if (uploadType == UploadType.Full)
            {
                await connection.ExecuteAsync($"TRUNCATE TABLE [{_table.TargetSchemaName}].[{_table.TargetTableName}]", transaction: transaction);
                await connection.ExecuteAsync(InsertCommand, transaction: transaction);
            }
            else if (uploadType == UploadType.DeleteMissing)
            {
                await connection.ExecuteAsync(DeleteCommand, transaction: transaction);
            }
            else
            {
                throw new NotSupportedException($"Unsupported UploadType {uploadType}");
            }
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        
    }

}
