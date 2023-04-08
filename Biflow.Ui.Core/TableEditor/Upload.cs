using Biflow.DataAccess.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Biflow.Ui.Core;

public class Upload
{
    private readonly List<Column> _columns;
    private readonly MasterDataTable _table;
    private readonly string[] _pkColumns;
    
    public ICollection<IDictionary<string, object?>> Data { get; }

    internal Upload(MasterDataTable table, List<Column> columns, ICollection<IDictionary<string, object?>> data)
    {
        _table = table;
        _columns = columns;
        Data = data;
        _pkColumns = _columns.Where(c => c.IsPrimaryKey).Select(c => c.Name).ToArray();
    }

    public async Task<(int Inserted, int Updated, int Deleted)> SaveUploadToDbAsync(UploadType uploadType)
    {
        using var connection = new SqlConnection(_table.Connection.ConnectionString);
        await connection.OpenAsync();

        var uploadedColumns = _columns.Where(c => !c.IsComputed).ToList();
        var columnDefinitions = uploadedColumns.Select(c => $"[{c.Name}] {c.DbCreateDatatype}");
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
        foreach (var column in uploadedColumns)
        {
            var mapping = new SqlBulkCopyColumnMapping(column.Name, column.Name);
            copy.ColumnMappings.Add(mapping);
        }
        var reader = new DictionaryReader(_columns.Select(c => c.Name), Data);
        await copy.WriteToServerAsync(reader);

        var (inserted, updated, deleted) = (0, 0, 0);
        var transaction = await connection.BeginTransactionAsync();
        try
        {
            switch (uploadType)
            {
                case UploadType.Upsert:
                    updated = await ExecuteUpdateAsync(connection, transaction);
                    inserted = await ExecuteInsertAsync(connection, transaction);
                    break;
                case UploadType.InsertNew:
                    inserted = await ExecuteInsertAsync(connection, transaction);
                    break;
                case UploadType.UpdateExisting:
                    updated = await ExecuteUpdateAsync(connection, transaction);
                    break;
                case UploadType.Full:
                    await connection.ExecuteAsync($"TRUNCATE TABLE [{_table.TargetSchemaName}].[{_table.TargetTableName}]", transaction: transaction);
                    inserted = await ExecuteInsertAsync(connection, transaction);
                    break;
                case UploadType.DeleteMissing:
                    deleted = await ExecuteDeleteAsync(connection, transaction);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported UploadType {uploadType}");
            }
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        return (inserted, updated, deleted);
    }

    private async Task<int> ExecuteInsertAsync(SqlConnection connection, IDbTransaction transaction)
    {
        if (!_table.AllowInsert)
        {
            throw new InvalidOperationException("Insert operation rejected because the table does not allow inserts. No changes were made.");
        }
        var insertColumns = _columns
            .Where(c => !c.IsIdentity && !c.IsComputed && !c.IsLocked)
            .Select(c => c.Name)
            .ToList();
        if (!insertColumns.Any())
        {
            throw new InvalidOperationException("No insertable columns detected. No changes were made.");
        }
        var insertCommand = $"""
            INSERT INTO [{_table.TargetSchemaName}].[{_table.TargetTableName}] (
            {string.Join(",\n", insertColumns.Select(c => $"[{c}]"))}
            )
            SELECT {string.Join(",\n", insertColumns.Select(c => $"src.[{c}]"))}
            FROM #temp AS src
                LEFT JOIN [{_table.TargetSchemaName}].[{_table.TargetTableName}] AS target ON
                    {string.Join(" AND ", _pkColumns.Select(c => $"target.[{c}] = src.[{c}]"))}
            WHERE {string.Join(" AND ", _pkColumns.Select(c => $"target.[{c}] IS NULL"))}
            """;
        return await connection.ExecuteAsync(insertCommand, transaction: transaction);
    }

    private async Task<int> ExecuteUpdateAsync(SqlConnection connection, IDbTransaction transaction)
    {
        var updateColumns = _columns
            .Where(c => !c.IsComputed && !c.IsPrimaryKey && !c.IsIdentity && !c.IsLocked)
            .Select(c => c.Name)
            .ToList();
        if (!updateColumns.Any())
        {
            throw new InvalidOperationException("No updateable columns detected. No changes were made.");
        }
        var updateCommand = $"""
            UPDATE target
            SET {string.Join(",\n", updateColumns.Select(c => $"[{c}] = src.[{c}]"))}
            FROM [{_table.TargetSchemaName}].[{_table.TargetTableName}] AS target
                INNER JOIN #temp AS src ON {string.Join(" AND ", _pkColumns.Select(c => $"target.[{c}] = src.[{c}]"))}
            WHERE NOT EXISTS (
                SELECT {string.Join(',', updateColumns.Select(c => $"target.[{c}]"))}
                INTERSECT
                SELECT {string.Join(',', updateColumns.Select(c => $"src.[{c}]"))}
            )
            """;
        return await connection.ExecuteAsync(updateCommand, transaction: transaction);
    }

    private async Task<int> ExecuteDeleteAsync(SqlConnection connection, IDbTransaction transaction)
    {
        if (!_table.AllowDelete)
        {
            throw new InvalidOperationException("Delete operation rejected because the table does not allow deletes. No changes were made.");
        }
        var deleteCommand = $"""
            DELETE FROM target
            FROM [{_table.TargetSchemaName}].[{_table.TargetTableName}] AS target
                LEFT JOIN #temp AS src ON {string.Join(" AND ", _pkColumns.Select(c => $"target.[{c}] = src.[{c}]"))}
            WHERE {string.Join(" AND ", _pkColumns.Select(c => $"src.[{c}] IS NULL"))}
            """;
        return await connection.ExecuteAsync(deleteCommand, transaction: transaction);
    }

}
