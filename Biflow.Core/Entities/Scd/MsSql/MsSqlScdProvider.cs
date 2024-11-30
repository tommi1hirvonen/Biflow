using System.Text;

namespace Biflow.Core.Entities.Scd.MsSql;

internal class MsSqlScdProvider(ScdTable table, IColumnMetadataProvider columnProvider) : IScdProvider
{
    private const string HashKeyColumn = "_HashKey";
    private const string ValidFromColumn = "_ValidFrom";
    private const string ValidUntilColumn = "_ValidUntil";
    private const string IsCurrentColumn = "_IsCurrent";
    private const string RecordHashColumn = "_RecordHash";
    private static readonly string[] SystemColumns =
    [
        HashKeyColumn,
        ValidFromColumn,
        ValidUntilColumn,
        IsCurrentColumn,
        RecordHashColumn
    ];
    public const int SysNameLength = 128;

    public async Task<StagingLoadStatementResult> CreateStagingLoadStatementAsync(
        CancellationToken cancellationToken = default)
    {
        var sourceColumns = (await columnProvider.GetTableColumnsAsync(
                table.SourceTableSchema, table.SourceTableName, cancellationToken))
            .Where(c => !SystemColumns.Contains(c.ColumnName))
            .Cast<IOrderedLoadColumn>()
            .ToArray();
        var targetColumns = (await columnProvider.GetTableColumnsAsync(
                table.TargetTableSchema, table.TargetTableName, cancellationToken))
            .Where(c => !SystemColumns.Contains(c.ColumnName))
            .Cast<IOrderedLoadColumn>()
            .ToArray();
        
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
            BEGIN TRY
            
            BEGIN TRANSACTION;
            
            DROP TABLE IF EXISTS {stagingTableName};
            
            SELECT {(table.SelectDistinct ? "DISTINCT" : null)}
            {string.Join(",\n", quotedStagingColumns)}
            INTO {stagingTableName}
            FROM {sourceTableName};
            
            COMMIT TRANSACTION;
            
            END TRY
            BEGIN CATCH
            
            ROLLBACK TRANSACTION;
            THROW;
                
            END CATCH;
            
            """;
        
        return new(statement, sourceColumns, targetColumns);
    }

    public async Task<string> CreateTargetLoadStatementAsync(CancellationToken cancellationToken = default)
    {
        var sourceColumns = (await columnProvider.GetTableColumnsAsync(
                table.SourceTableSchema, table.SourceTableName, cancellationToken))
            .Where(c => !SystemColumns.Contains(c.ColumnName))
            .Cast<IOrderedLoadColumn>()
            .ToArray();
        var targetColumns = (await columnProvider.GetTableColumnsAsync(
                table.TargetTableSchema, table.TargetTableName, cancellationToken))
            .Where(c => !SystemColumns.Contains(c.ColumnName))
            .Cast<IOrderedLoadColumn>()
            .ToArray();
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
            BEGIN TRY
            
            BEGIN TRANSACTION;
            
            {table.PreLoadScript}
            
            """);
        
        var update = table.FullLoad
            ? $"""
               UPDATE tgt
               SET {IsCurrentColumn.QuoteName()} = 0, {ValidUntilColumn.QuoteName()} = GETDATE()
               FROM {targetTableName} AS tgt
               LEFT JOIN {stagingTableName} AS src ON tgt.{HashKeyColumn.QuoteName()} = src.{HashKeyColumn.QuoteName()}
               WHERE tgt.{IsCurrentColumn.QuoteName()} = 1 AND
                     (tgt.{RecordHashColumn.QuoteName()} <> src.{RecordHashColumn.QuoteName()} OR src.{RecordHashColumn.QuoteName()} IS NULL);
                       
               """
            : $"""
               UPDATE tgt
               SET {IsCurrentColumn.QuoteName()} = 0, {ValidUntilColumn.QuoteName()} = GETDATE()
               FROM {targetTableName} AS tgt
               INNER JOIN {stagingTableName} AS src ON tgt.{HashKeyColumn.QuoteName()} = src.{HashKeyColumn.QuoteName()}
               WHERE tgt.{IsCurrentColumn.QuoteName()} = 1 AND
                     tgt.{RecordHashColumn.QuoteName()} <> src.{RecordHashColumn.QuoteName()};
                     
               """;
        builder.AppendLine(update);
        
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
            
            COMMIT TRANSACTION;
            
            END TRY
            BEGIN CATCH
            
            ROLLBACK TRANSACTION;
            THROW;
                
            END CATCH;
            """);
        
        return builder.ToString();
    }

    public async Task<string> CreateStructureUpdateStatementAsync(CancellationToken cancellationToken = default)
    {
        var sourceColumns = (await columnProvider.GetTableColumnsAsync(
                table.SourceTableSchema, table.SourceTableName, cancellationToken))
            .Where(c => !SystemColumns.Contains(c.ColumnName))
            .Cast<IStructureColumn>()
            .ToArray();
        var targetColumns = (await columnProvider.GetTableColumnsAsync(
                table.TargetTableSchema, table.TargetTableName, cancellationToken))
            .Where(c => !SystemColumns.Contains(c.ColumnName))
            .Cast<IStructureColumn>()
            .ToArray();
        
        Scd.EnsureScdTableValidated(
            table,
            sourceColumns.Cast<IColumn>().ToArray(),
            targetColumns.Cast<IColumn>().ToArray());
        
        var builder = new StringBuilder();
        
        var tableName = $"{table.TargetTableSchema.QuoteName()}.{table.TargetTableName.QuoteName()}";
        
        if (targetColumns.Length == 0)
        {
            // Target table does not exist.
            var columnDefinitions = GetStructureColumns(sourceColumns)
                .Select(c => $"{c.ColumnName.QuoteName()} {c.DataType} {(c.IsNullable ? "null" : "not null")}");
            
            builder.AppendLine($"""
                CREATE TABLE {tableName} (
                {string.Join(",\n", columnDefinitions)}
                );

                """);

            if (!table.ApplyIndexesOnCreate)
            {
                return builder.ToString();
            }
            
            var nkIndexName = $"IX_{table.TargetTableName}_NaturalKey".QuoteName();
            var nkIndexColumnDefinitions = table.NaturalKeyColumns
                .Distinct()
                .Order()
                .Select(c => $"{c.QuoteName()} ASC")
                .Append($"{IsCurrentColumn.QuoteName()} ASC");
            var hkIndexName = $"IX_{table.TargetTableName}_HashKey".QuoteName();
            var hkIndexColumnDefinition = $"{HashKeyColumn.QuoteName()} ASC, {IsCurrentColumn.QuoteName()} ASC";
            builder.AppendLine($"""
                CREATE CLUSTERED INDEX {hkIndexName} ON {tableName} (
                {hkIndexColumnDefinition}
                );

                CREATE NONCLUSTERED INDEX {nkIndexName} ON {tableName} (
                {string.Join(",\n", nkIndexColumnDefinitions)}
                );

                """);

            return builder.ToString();
        }

        IEnumerable<IStructureColumn> columnsToAlter;
        IEnumerable<IStructureColumn> columnsToAdd;

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
                            .All(c2 => c1.ColumnName != c2));
                // Newly included columns
                columnsToAdd = sourceColumns
                    .Where(c => disabled.IncludedColumns.Contains(c.ColumnName))
                    .Where(c => targetColumns.All(sc => sc.ColumnName != c.ColumnName));
                
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
                    : [];
                
                break;
            }
            default:
                throw new ScdTableValidationException($"Unknown table type: {table.GetType().Name}");
        }

        foreach (var c in columnsToAlter)
        {
            builder.AppendLine($"ALTER TABLE {tableName} ALTER COLUMN {c.ColumnName.QuoteName()} {c.DataType} NULL;"); 
        }

        foreach (var c in columnsToAdd.OrderBy(c => c.ColumnName))
        {
            // New columns are nullable because target table might already contain data
            builder.AppendLine($"ALTER TABLE {tableName} ADD {c.ColumnName.QuoteName()} {c.DataType} NULL;");
        }
        
        return builder.ToString();
    }
    
    private IStructureColumn[] GetStructureColumns(IStructureColumn[] sourceColumns)
    {
        var hashKeyColumn = new StructureColumn
        {
            ColumnName = HashKeyColumn,
            DataType = "varchar(32)",
            IsNullable = false
        };
        
        var naturalKeyColumns = table.NaturalKeyColumns
            .Distinct()
            .Order() // Use alphabetical ordering for columns when listing them for table structure. 
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
            DataType = "datetime2(6)",
            IsNullable = false
        };
        var validUntil = new StructureColumn
        {
            ColumnName = ValidUntilColumn,
            DataType = "datetime2(6)",
            IsNullable = true
        };
        var isCurrent = new StructureColumn
        {
            ColumnName = IsCurrentColumn,
            DataType = "bit",
            IsNullable = false
        };
        
        var recordHashColumn = new StructureColumn
        {
            ColumnName = RecordHashColumn,
            DataType = "VARCHAR(32)",
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
            $"CONVERT(VARCHAR(32), HASHBYTES('MD5', CONCAT({string.Join(", '|', ", quotedNkColumns)})), 2)";
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
            TargetTableExpression = "CONVERT(datetime2(6), '9999-12-31')"
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
            // HASHBYTES() is the most reliable way of case-sensitively capturing all changes.
            StagingTableExpression = $"CONVERT(VARCHAR(32), HASHBYTES('MD5', CONCAT({string.Join(", '|', ", quotedRecordColumns)})), 2)",
            TargetTableExpression = null
        };
        
        return [hashKeyColumn, ..naturalKeyColumns, validFrom, validUntil, isCurrent, recordHashColumn, ..otherColumns];
    }
}

file static class Extensions
{
    /// <summary>
    /// Returns a string with the delimiters added to make the input string
    /// a valid SQL Server delimited identifier. Unlike the T-SQL version,
    /// an ArgumentException is thrown instead of returning a null for
    /// invalid arguments.
    /// </summary>
    /// <param name="name">sysname, limited to 128 characters.</param>
    /// <param name="quoteCharacter">Can be a single quotation mark ( ' ), a
    /// left or right bracket ( [] ), or a double quotation mark ( " ).</param>
    /// <returns>An escaped identifier, no longer than 258 characters.</returns>
    public static string QuoteName(this string name, char quoteCharacter = '[') => (name, quoteCharacter) switch
    {
        ({ Length: > MsSqlScdProvider.SysNameLength }, _) =>
            throw new ArgumentException($"{nameof(name)} is longer than {MsSqlScdProvider.SysNameLength} characters"),
        (_, '\'') => $"'{name.Replace("'", "''")}'",
        (_, '"') => $"\"{name.Replace("\"", "\"\"")}\"",
        (_, '[' or ']') => $"[{name.Replace("]", "]]")}]",
        _ => throw new ArgumentException("quoteCharacter must be one of: ', \", [, or ]"),
    };
}