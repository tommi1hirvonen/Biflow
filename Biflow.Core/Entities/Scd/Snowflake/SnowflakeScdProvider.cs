using System.Text;

namespace Biflow.Core.Entities.Scd.Snowflake;

internal class SnowflakeScdProvider(ScdTable table, IColumnMetadataProvider columnProvider) : IScdProvider
{
    private const string HashKeyColumn = "_HASH_KEY";
    private const string ValidFromColumn = "_VALID_FROM";
    private const string ValidUntilColumn = "_VALID_UNTIL";
    private const string IsCurrentColumn = "_IS_CURRENT";
    private const string RecordHashColumn = "_RECORD_HASH";
    private static readonly string[] SystemColumns =
    [
        HashKeyColumn,
        ValidFromColumn,
        ValidUntilColumn,
        IsCurrentColumn,
        RecordHashColumn
    ];

    public async Task<StagingLoadStatementResult> CreateStagingLoadStatementAsync(
        CancellationToken cancellationToken = default)
    {
        var (sourceColumns, targetColumns) = await GetSourceTargetColumnsAsync<IOrderedLoadColumn>(cancellationToken);
        
        Scd.EnsureScdTableValidated(
            table,
            sourceColumns.Cast<IColumn>().ToArray(),
            targetColumns.Cast<IColumn>().ToArray());
        
        var includedColumns = GetDataLoadColumns(sourceColumns, targetColumns);
        
        var sourceTableName = $"{table.SourceTableSchema.QuoteName()}.{table.SourceTableName.QuoteName()}";
        var stagingTableName = string.IsNullOrEmpty(table.StagingTableSchema)
            ? $"{table.StagingTableName.QuoteName()}"
            : $"{table.StagingTableSchema.QuoteName()}.{table.StagingTableName.QuoteName()}";
        var quotedStagingColumns = includedColumns
            .Where(c => c.IncludeInStagingTable)
            .Select(c => c.StagingTableExpression is null
                ? c.ColumnName.QuoteName()
                : $"{c.StagingTableExpression} AS {c.ColumnName.QuoteName()}");
        
        var statement = $"""
            BEGIN
            
            DROP TABLE IF EXISTS {stagingTableName};
            
            CREATE TABLE {stagingTableName} AS
            SELECT {(table.SelectDistinct ? "DISTINCT" : null)}
            {string.Join(",\n", quotedStagingColumns)}
            FROM {sourceTableName};
            
            END;
            
            """;
        
        return new(statement, sourceColumns, targetColumns);
    }
    
    public async Task<string> CreateTargetLoadStatementAsync(CancellationToken cancellationToken = default)
    {
        var (sourceColumns, targetColumns) = await GetSourceTargetColumnsAsync<IOrderedLoadColumn>(cancellationToken);
        return CreateTargetLoadStatement(sourceColumns, targetColumns);
    }
    
    public string CreateTargetLoadStatement(
        IReadOnlyList<IOrderedLoadColumn> sourceColumns,
        IReadOnlyList<IOrderedLoadColumn> targetColumns)
    {
        Scd.EnsureScdTableValidated(
            table,
            sourceColumns.Cast<IColumn>().ToArray(),
            targetColumns.Cast<IColumn>().ToArray());
        
        var builder = new StringBuilder();
        var includedColumns = GetDataLoadColumns(sourceColumns, targetColumns);
        
        var targetTableName = $"{table.TargetTableSchema.QuoteName()}.{table.TargetTableName.QuoteName()}";
        var stagingTableName = string.IsNullOrEmpty(table.StagingTableSchema)
            ? $"{table.StagingTableName.QuoteName()}"
            : $"{table.StagingTableSchema.QuoteName()}.{table.StagingTableName.QuoteName()}";
        var quotedInsertColumns = includedColumns
            .Select(c => c.ColumnName.QuoteName());
        var quotedSelectColumns = includedColumns
            .Select(c => c.TargetTableExpression is null
                ? $"src.{c.ColumnName.QuoteName()}"
                : $"{c.TargetTableExpression} AS {c.ColumnName.QuoteName()}");
        
        builder.AppendLine($"""
            BEGIN
            
            BEGIN TRANSACTION;
            
            {table.PreLoadScript}
            
            """);

        if (table.FullLoad)
        {
            // Update removed records validity.
            builder.AppendLine($"""
                UPDATE {targetTableName} AS tgt
                SET {IsCurrentColumn.QuoteName()} = 0, {ValidUntilColumn.QuoteName()} = CURRENT_TIMESTAMP
                WHERE tgt.{IsCurrentColumn.QuoteName()} = 1 AND
                      NOT EXISTS (
                          SELECT *
                          FROM {stagingTableName} AS src
                          WHERE tgt.{HashKeyColumn.QuoteName()} = src.{HashKeyColumn.QuoteName()}
                      );
                      
                """);    
        }

        // Update changed records validity.
        builder.AppendLine($"""
            UPDATE {targetTableName} AS tgt
            SET {IsCurrentColumn.QuoteName()} = 0, {ValidUntilColumn.QuoteName()} = CURRENT_TIMESTAMP
            FROM {stagingTableName} AS src
            WHERE tgt.{IsCurrentColumn.QuoteName()} = 1 AND
                  tgt.{HashKeyColumn.QuoteName()} = src.{HashKeyColumn.QuoteName()} AND -- inner join
                  tgt.{RecordHashColumn.QuoteName()} <> src.{RecordHashColumn.QuoteName()};
                  
            """);
        
        // Insert new and changed records.
        builder.AppendLine($"""
            INSERT INTO {targetTableName} (
            {string.Join(",\n", quotedInsertColumns)}
            )
            SELECT
            {string.Join(",\n", quotedSelectColumns)}
            FROM {stagingTableName} AS src
                LEFT JOIN {targetTableName} AS tgt ON
                    src.{HashKeyColumn.QuoteName()} = tgt.{HashKeyColumn.QuoteName()} AND
                    tgt.{IsCurrentColumn.QuoteName()} = 1
            WHERE tgt.{HashKeyColumn.QuoteName()} IS NULL OR 
                  src.{RecordHashColumn.QuoteName()} <> tgt.{RecordHashColumn.QuoteName()};
                  
            """);
        
        builder.AppendLine($"""
            {table.PostLoadScript}
            
            COMMIT;
            
            EXCEPTION
                WHEN OTHER THEN
            
                ROLLBACK;
                RAISE;
                
            END;
            """);
        
        return builder.ToString();
    }
    
    public async Task<string> CreateStructureUpdateStatementAsync(CancellationToken cancellationToken = default)
    {
        var (sourceColumns, targetColumns) = await GetSourceTargetColumnsAsync<IStructureColumn>(cancellationToken);
        
        Scd.EnsureScdTableValidated(
            table,
            sourceColumns.Cast<IColumn>().ToArray(),
            targetColumns.Cast<IColumn>().ToArray());
        
        var builder = new StringBuilder();
        
        var tableName = $"{table.TargetTableSchema.QuoteName()}.{table.TargetTableName.QuoteName()}";
        
        if (targetColumns.Count == 0)
        {
            // Target table does not exist.
            var columnDefinitions = GetStructureColumns(sourceColumns)
                .Select(c => $"{c.ColumnName.QuoteName()} {c.DataType} {(c.IsNullable ? "NULL" : "NOT NULL")}");
            builder.AppendLine($"""
                CREATE TABLE {tableName} (
                {string.Join(",\n", columnDefinitions)}
                );

                """);
            return builder.ToString();
        }
        
        IStructureColumn[] columnsToAlter, columnsToAdd;

        switch (table.SchemaDriftConfiguration)
        {
            case SchemaDriftDisabledConfiguration disabled:
            {
                // Newly excluded columns (removed from included)
                columnsToAlter = targetColumns
                    .Where(c => !c.IsNullable)
                    .Where(c => !SystemColumns.Contains(c.ColumnName))
                    .Where(c1 =>
                        disabled.IncludedColumns.Concat(table.NaturalKeyColumns)
                            .All(c2 => c1.ColumnName != c2))
                    .ToArray();
                // Newly included columns
                columnsToAdd = sourceColumns
                    .Where(c => disabled.IncludedColumns.Contains(c.ColumnName))
                    .Where(c => targetColumns.All(sc => sc.ColumnName != c.ColumnName))
                    .ToArray();
                
                break;
            }
            case SchemaDriftEnabledConfiguration enabled:
            {
                // In case table silently ignores removed columns.
                var missingNonNullColumns = targetColumns
                    .Where(c => !enabled.ExcludedColumns.Contains(c.ColumnName))
                    .Where(c => sourceColumns.All(sc => sc.ColumnName != c.ColumnName))
                    .Where(c => !c.IsNullable);
                var newlyExcludedColumns = targetColumns
                    .Where(c => !c.IsNullable)
                    .Where(c => !SystemColumns.Contains(c.ColumnName))
                    .Where(c => enabled.ExcludedColumns.Contains(c.ColumnName));
                columnsToAlter = [..missingNonNullColumns, ..newlyExcludedColumns];
                columnsToAdd = enabled.IncludeNewColumns
                    ? sourceColumns
                        .Where(c => !enabled.ExcludedColumns.Contains(c.ColumnName))
                        .Where(c => targetColumns.All(sc => sc.ColumnName != c.ColumnName))
                        .ToArray()
                    : [];
                
                break;
            }
            default:
                throw new ScdTableValidationException($"Unknown table type: {table.GetType().Name}");
        }

        if (columnsToAlter.Length != 0 || columnsToAdd.Length != 0)
        {
            builder.AppendLine("BEGIN\n");
        }
        
        foreach (var c in columnsToAlter)
        {
            builder.AppendLine($"ALTER TABLE {tableName} ALTER {c.ColumnName.QuoteName()} DROP NOT NULL;"); 
        }

        foreach (var c in columnsToAdd.OrderBy(c => c.ColumnName))
        {
            // New columns are nullable because target table might already contain data
            builder.AppendLine($"ALTER TABLE {tableName} ADD {c.ColumnName.QuoteName()} {c.DataType} NULL;");
        }
        
        if (columnsToAlter.Length != 0 || columnsToAdd.Length != 0)
        {
            builder.AppendLine("END;\n");
        }
        
        return builder.ToString();
    }
    
    private IReadOnlyList<IStructureColumn> GetStructureColumns(IReadOnlyList<IStructureColumn> sourceColumns)
    {
        var hashKeyColumn = new StructureColumn
        {
            ColumnName = HashKeyColumn,
            DataType = "VARCHAR(32)",
            IsNullable = false
        };
        
        var naturalKeyColumns = table.NaturalKeyColumns
            .Distinct()
            .Order()
            .Select(c =>
            {
                var sourceColumn = sourceColumns.First(sc => sc.ColumnName == c); 
                return new StructureColumn
                {
                    ColumnName = sourceColumn.ColumnName,
                    DataType = sourceColumn.DataType,
                    IsNullable = false // force natural key column to be non-null
                };
            })
            .ToArray();
        
        var validFrom = new StructureColumn
        {
            ColumnName = ValidFromColumn,
            DataType = "TIMESTAMP_NTZ",
            IsNullable = false
        };
        var validUntil = new StructureColumn
        {
            ColumnName = ValidUntilColumn,
            DataType = "TIMESTAMP_NTZ",
            IsNullable = true
        };
        var isCurrent = new StructureColumn
        {
            ColumnName = IsCurrentColumn,
            DataType = "BOOLEAN",
            IsNullable = false
        };
        
        var otherColumns = table.SchemaDriftConfiguration switch
        {
            SchemaDriftDisabledConfiguration disabled =>
                Scd.GetNonNkIncludedColumns(table.NaturalKeyColumns, disabled, sourceColumns)
                    .OrderBy(c => c.ColumnName).ToArray(),
            SchemaDriftEnabledConfiguration enabled =>
                Scd.GetNonNkStructureIncludedColumns(table.NaturalKeyColumns, enabled, sourceColumns)
                    .OrderBy(c => c.ColumnName).ToArray(),
            _ => throw new ScdTableValidationException($"Unhandled table historization case: {table.GetType().Name}")
        };
        
        var recordHashColumn = new StructureColumn
        {
            ColumnName = RecordHashColumn,
            DataType = "VARCHAR(32)",
            IsNullable = false
        };
        
        return [hashKeyColumn, ..naturalKeyColumns, validFrom, validUntil, isCurrent, recordHashColumn, ..otherColumns];
    }
    
    private ILoadColumn[] GetDataLoadColumns(
        IReadOnlyList<IOrderedLoadColumn> sourceColumns, IReadOnlyList<IOrderedLoadColumn> targetColumns)
    {
        var quotedNkColumns = table.NaturalKeyColumns
            .Select(c => targetColumns.First(sc => sc.ColumnName == c))
            .DistinctBy(c => c.ColumnName)
            .OrderBy(c => c.Ordinal) // Order by ordinal to make sure column renames don't affect hashing results.
            .Select(c => c.ColumnName)
            .Select(c => c.QuoteName())
            .ToArray();
        var hashKeyExpression =
            $"UPPER(MD5(CONCAT({string.Join(", '|', ", quotedNkColumns)})))";
        var hashKeyColumn = new LoadColumn
        {
            ColumnName = HashKeyColumn,
            IncludeInStagingTable = true,
            StagingTableExpression = hashKeyExpression,
            TargetTableExpression = null
        };
        var validFrom = new LoadColumn
        {
            ColumnName = ValidFromColumn,
            IncludeInStagingTable = false,
            StagingTableExpression = null,
            TargetTableExpression = "GETDATE()"
        };
        var validUntil = new LoadColumn
        {
            ColumnName = ValidUntilColumn,
            IncludeInStagingTable = false,
            StagingTableExpression = null,
            TargetTableExpression = "CAST('9999-12-31' AS TIMESTAMP_NTZ)"
        };
        var isCurrent = new LoadColumn
        {
            ColumnName = IsCurrentColumn,
            IncludeInStagingTable = false,
            StagingTableExpression = null,
            TargetTableExpression = "1"
        };
        
        var naturalKeyColumns = table.NaturalKeyColumns
            .Select(c => targetColumns.First(sc => sc.ColumnName == c))
            .DistinctBy(c => c.ColumnName)
            .OrderBy(c => c.Ordinal) // Order by ordinal to make sure column renames don't affect hashing results.
            .ToArray();
        
        // Order by ordinal to make sure column renames do not affect how hashes are calculated.
        var otherColumns = table.SchemaDriftConfiguration switch
        {
            SchemaDriftDisabledConfiguration disabled =>
                Scd.GetNonNkIncludedColumns(table.NaturalKeyColumns, disabled, targetColumns)
                    .OrderBy(c => c.Ordinal).ToArray(),
            SchemaDriftEnabledConfiguration enabled =>
                Scd.GetNonNkLoadIncludedColumns(table.NaturalKeyColumns, enabled, sourceColumns, targetColumns)
                    .OrderBy(c => c.Ordinal).ToArray(),
            _ => throw new ScdTableValidationException($"Unhandled table historization case: {table.GetType().Name}")
        };
        
        IEnumerable<ILoadColumn> recordColumns = [..naturalKeyColumns, ..otherColumns];
        var quotedRecordColumns = recordColumns
            .Select(c => c.ColumnName.QuoteName())
            .ToArray();
        var recordHashColumn = new LoadColumn
        {
            ColumnName = RecordHashColumn,
            IncludeInStagingTable = true,
            // MD5 is the most reliable way of case-sensitively capturing all changes.
            StagingTableExpression = $"UPPER(MD5(CONCAT({string.Join(", '|', ", quotedRecordColumns)})))",
            TargetTableExpression = null
        };
        
        return [hashKeyColumn, ..naturalKeyColumns, validFrom, validUntil, isCurrent, recordHashColumn, ..otherColumns];
    }

    private async Task<(IReadOnlyList<T> SourceColumns, IReadOnlyList<T> TargetColumns)>
        GetSourceTargetColumnsAsync<T>(CancellationToken cancellationToken)
    {
        var sourceColumns = (await columnProvider.GetColumnsAsync(
                table.SourceTableSchema, table.SourceTableName, cancellationToken))
            .Where(c => !SystemColumns.Contains(c.ColumnName))
            .Cast<T>()
            .ToArray();
        var targetColumns = (await columnProvider.GetColumnsAsync(
                table.TargetTableSchema, table.TargetTableName, cancellationToken))
            .Where(c => !SystemColumns.Contains(c.ColumnName))
            .Cast<T>()
            .ToArray();
        return (sourceColumns, targetColumns);
    }
}

file static class Extensions
{
    // TODO: Handle potential quoted object identifiers
    // https://docs.snowflake.com/en/sql-reference/identifiers-syntax
    public static string QuoteName(this string name) => name;
}