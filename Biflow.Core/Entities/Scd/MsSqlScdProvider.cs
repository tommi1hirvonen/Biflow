using System.Text;

namespace Biflow.Core.Entities;

public class MsSqlScdProvider(ScdTable table, IScdColumnMetadataProvider columnProvider) : IScdProvider
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
    
    public async Task<string> CreateDataLoadStatementAsync(CancellationToken cancellationToken = default)
    {
        var sourceColumnMetadata = await columnProvider.GetTableColumnsAsync(
            table.Connection.ConnectionString,
            table.SourceTableSchema,
            table.SourceTableName,
            cancellationToken);
        var sourceColumns = sourceColumnMetadata
            .Where(c => !SystemColumns.Contains(c.ColumnName))
            .ToArray();
        var builder = new StringBuilder();
        var includedColumns = GetIncludedColumns(sourceColumns);
        
        var targetTableName = $"{table.TargetTableSchema.QuoteName()}.{table.TargetTableName.QuoteName()}";
        var sourceTableName = $"{table.SourceTableSchema.QuoteName()}.{table.SourceTableName.QuoteName()}";
        var stagingTableName = string.IsNullOrEmpty(table.StagingTableSchema)
            ? $"{table.StagingTableName.QuoteName()}"
            : $"{table.StagingTableSchema.QuoteName()}.{table.StagingTableName.QuoteName()}";
        var quotedStagingColumns = includedColumns
            .Where(c => c.IncludeInStagingTable)
            .Select(c => c.StagingTableExpression is null
                ? c.ColumnName.QuoteName()
                : $"{c.StagingTableExpression} AS {c.ColumnName.QuoteName()}");
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

        builder.AppendLine($"""
                            DROP TABLE IF EXISTS {stagingTableName};

                            SELECT {(table.SelectDistinct ? "DISTINCT" : null)}
                            {string.Join(",\n", quotedStagingColumns)}    
                            INTO {stagingTableName}
                            FROM {sourceTableName};
                            
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
        if (table.SourceTableSchema == table.TargetTableSchema && table.SourceTableName == table.TargetTableName)
        {
            throw new ScdTableValidationException("The target and source table cannot be the same");
        }
        
        if (table.NaturalKeyColumns.Count == 0)
        {
            throw new ScdTableValidationException("The table must have at least one natural key column.");
        }
        
        var sourceColumnMetadata = await columnProvider.GetTableColumnsAsync(
            table.Connection.ConnectionString,
            table.SourceTableSchema,
            table.SourceTableName,
            cancellationToken);
        var sourceColumns = sourceColumnMetadata
            .Where(c => !SystemColumns.Contains(c.ColumnName))
            .ToArray();

        if (sourceColumns.Length == 0)
        {
            throw new ScdTableValidationException("The source table must have at least one column.");
        }

        var missingNaturalKeyColumns = table.NaturalKeyColumns
            .Where(c => sourceColumns.All(sc => sc.ColumnName != c))
            .ToArray();
        if (missingNaturalKeyColumns.Length > 0)
        {
            var columns = string.Join(", ", missingNaturalKeyColumns);
            throw new ScdTableValidationException($"Natural key columns are missing from the source table: {columns}");
        }
        
        var targetColumnMetadata = await columnProvider.GetTableColumnsAsync(
            table.Connection.ConnectionString,
            table.TargetTableSchema,
            table.TargetTableName,
            cancellationToken);
        var targetColumns = targetColumnMetadata
            .Where(c => !SystemColumns.Contains(c.ColumnName))
            .ToArray();
        
        var builder = new StringBuilder();
        
        var tableName = $"{table.TargetTableSchema.QuoteName()}.{table.TargetTableName.QuoteName()}";
        
        if (targetColumns.Length == 0)
        {
            // Target table does not exist.
            var nkIndexName = $"IX_{table.TargetTableName}_NaturalKey".QuoteName();
            var nkIndexColumnDefinitions = table.NaturalKeyColumns
                .Distinct()
                .Order()
                .Select(c => $"{c.QuoteName()} ASC")
                .Append($"{IsCurrentColumn.QuoteName()} ASC");
            var hkIndexName = $"IX_{table.TargetTableName}_HashKey".QuoteName();
            var hkIndexColumnDefinition = $"{HashKeyColumn.QuoteName()} ASC, {IsCurrentColumn.QuoteName()} ASC";
            var columnDefinitions = GetIncludedColumns(sourceColumns)
                .Select(c => $"{c.ColumnName.QuoteName()} {c.DataType} {(c.IsNullable ? "null" : "not null")}");
            
            builder.AppendLine($"""
                CREATE TABLE {tableName} (
                {string.Join(",\n", columnDefinitions)}
                );

                """);

            if (table.ApplyIndexesOnCreate)
            {
                builder.AppendLine($"""
                    CREATE CLUSTERED INDEX {hkIndexName} ON {tableName} (
                    {hkIndexColumnDefinition}
                    );

                    CREATE NONCLUSTERED INDEX {nkIndexName} ON {tableName} (
                    {string.Join(",\n", nkIndexColumnDefinitions)}
                    );

                    """);
            }
            
            return builder.ToString();
        }

        switch (table.SchemaDriftConfiguration)
        {
            case SchemaDriftDisabledConfiguration schemaDriftDisabled:
            {
                var missingColumns = schemaDriftDisabled.IncludedColumns
                    .Where(c => sourceColumns.All(sc => sc.ColumnName != c))
                    .ToArray();
                if (missingColumns.Length > 0)
                {
                    var missingColumnNames = string.Join(", ", missingColumns);
                    throw new ScdTableValidationException(
                        $"Schema drift was disabled and some included columns are missing from the source table: {missingColumnNames}");    
                }
                
                // Handle any potential newly excluded columns by making them nullable.
                var newlyExcludedColumns = targetColumns
                    .Where(c => !c.IsNullable)
                    .Where(c => !SystemColumns.Contains(c.ColumnName))
                    .Where(c1 =>
                        schemaDriftDisabled.IncludedColumns.Concat(table.NaturalKeyColumns)
                            .All(c2 => c1.ColumnName != c2));
                foreach (var column in newlyExcludedColumns)
                {
                    builder.AppendLine($"""
                        ALTER TABLE {tableName}
                            ALTER COLUMN {column.ColumnName.QuoteName()} {column.DataType} NULL;
                        
                        """);       
                }
                
                break;
            }
            case SchemaDriftEnabledConfiguration schemaDriftEnabled:
            {
                var missingColumns = targetColumns
                    .Where(c => !schemaDriftEnabled.ExcludedColumns.Contains(c.ColumnName))
                    .Where(c => sourceColumns.All(sc => sc.ColumnName != c.ColumnName))
                    .ToArray();
                if (!schemaDriftEnabled.IgnoreMissingColumns && missingColumns.Length > 0)
                {
                    // Table does not handle missing columns and there are some.
                    var missingColumnNames = string.Join(", ", missingColumns.Select(c => c.ColumnName.QuoteName()));
                    throw new ScdTableValidationException(
                        $"Schema drift enabled table is set to not handle removed columns and some columns are missing from the source: {missingColumnNames}");
                }

                if (missingColumns.Length > 0)
                {
                    // Table handles missing columns. Make sure the corresponding target columns are nullable.
                    var missingNonNullColumns = missingColumns
                        .Where(c => !c.IsNullable)
                        .ToArray();
                    foreach (var col in missingNonNullColumns)
                    {
                        builder.AppendLine($"""
                            ALTER TABLE {tableName} ALTER COLUMN {col.ColumnName.QuoteName()} {col.DataType} NULL;

                            """);
                    }
                }
                
                // Handle any potential newly excluded columns by making them nullable.
                var newlyExcludedColumns = targetColumns
                    .Where(c => !c.IsNullable)
                    .Where(c => !SystemColumns.Contains(c.ColumnName))
                    .Where(c => schemaDriftEnabled.ExcludedColumns.Contains(c.ColumnName));
                foreach (var column in newlyExcludedColumns)
                {
                    builder.AppendLine($"""
                        ALTER TABLE {tableName}
                            ALTER COLUMN {column.ColumnName.QuoteName()} {column.DataType} NULL;
                        
                        """);       
                }

                var columnsToAdd = sourceColumns
                    .Where(c => !schemaDriftEnabled.ExcludedColumns.Contains(c.ColumnName))
                    .Where(c => targetColumns.All(sc => sc.ColumnName != c.ColumnName))
                    .OrderBy(c => c.ColumnName)
                    .ToArray();
                if (schemaDriftEnabled.IncludeNewColumns && columnsToAdd.Length > 0)
                {
                    var columnDefinitions = columnsToAdd
                        .Select(c => $"{c.ColumnName.QuoteName()} {c.DataType} null"); // new columns are nullable because target table might already contain data
                    builder.AppendLine($"""
                        ALTER TABLE {tableName} ADD
                        {string.Join(",\n", columnDefinitions)};

                        """);
                }
                
                break;
            }
            default:
                throw new ScdTableValidationException($"Unknown table type: {table.GetType().Name}");
        }
        
        return builder.ToString();
    }
    
    private IReadOnlyList<ScdColumnMetadata> GetIncludedColumns(ScdColumnMetadata[] sourceColumns)
    {
        var quotedNkColumns = table.NaturalKeyColumns
            .Distinct()
            .Order()
            .Select(c => c.QuoteName())
            .ToArray();
        var hashKeyExpression =
            $"CONVERT(VARCHAR(32), HASHBYTES('MD5', CONCAT({string.Join(", '|', ", quotedNkColumns)})), 2)";
        var hashKeyColumn = new ScdColumnMetadata
        {
            ColumnName = HashKeyColumn,
            DataType = "varchar(32)",
            IsNullable = false,
            IncludeInStagingTable = true,
            StagingTableExpression = hashKeyExpression,
            TargetTableExpression = null
        };
        var validFrom = new ScdColumnMetadata
        {
            ColumnName = ValidFromColumn,
            DataType = "datetime2(6)",
            IsNullable = false,
            IncludeInStagingTable = false,
            StagingTableExpression = null,
            TargetTableExpression = "GETDATE()"
        };
        var validUntil = new ScdColumnMetadata
        {
            ColumnName = ValidUntilColumn,
            DataType = "datetime2(6)",
            IsNullable = true,
            IncludeInStagingTable = false,
            StagingTableExpression = null,
            TargetTableExpression = "CONVERT(datetime2(6), '9999-12-31')"
        };
        var isCurrent = new ScdColumnMetadata
        {
            ColumnName = IsCurrentColumn,
            DataType = "bit",
            IsNullable = false,
            IncludeInStagingTable = false,
            StagingTableExpression = null,
            TargetTableExpression = "1"
        };
        
        var naturalKeyColumns = table.NaturalKeyColumns
            .Distinct()
            .Order()
            .Select(c =>
            {
                var sourceColumn = sourceColumns.First(sc => sc.ColumnName == c); 
                return new ScdColumnMetadata
                {
                    ColumnName = sourceColumn.ColumnName,
                    DataType = sourceColumn.DataType,
                    IsNullable = false, // force natural key column to be non-null
                    IncludeInStagingTable = true,
                    StagingTableExpression = null,
                    TargetTableExpression = null
                };
            })
            .ToArray();
        
        var otherColumns = table.SchemaDriftConfiguration switch
        {
            SchemaDriftDisabledConfiguration disabled => GetNonNkIncludedColumns(disabled, sourceColumns),
            SchemaDriftEnabledConfiguration enabled => GetNonNkIncludedColumns(enabled, sourceColumns),
            _ => throw new ScdTableValidationException($"Unhandled table historization case: {table.GetType().Name}")
        };
        
        ScdColumnMetadata[] recordColumns = [..naturalKeyColumns, ..otherColumns];
        var quotedRecordColumns = recordColumns
            .Select(c => c.ColumnName.QuoteName())
            .ToArray();
        var recordHashColumn = new ScdColumnMetadata
        {
            ColumnName = RecordHashColumn,
            DataType = "VARCHAR(32)",
            IsNullable = false,
            IncludeInStagingTable = true,
            // HASHBYTES() is the most reliable way of case-sensitively capturing all changes.
            StagingTableExpression = $"CONVERT(VARCHAR(32), HASHBYTES('MD5', CONCAT({string.Join(", '|', ", quotedRecordColumns)})))",
            TargetTableExpression = null
        };
        
        return [hashKeyColumn, ..naturalKeyColumns, validFrom, validUntil, isCurrent, recordHashColumn, ..otherColumns];
    }
    
    private ScdColumnMetadata[] GetNonNkIncludedColumns(
        SchemaDriftDisabledConfiguration configuration,
        ScdColumnMetadata[] sourceColumns)
    {
        var missingColumns = configuration.IncludedColumns
            .Where(c => sourceColumns.All(sc => sc.ColumnName != c))
            .ToArray();
        
        if (missingColumns.Length <= 0)
        {
            return configuration.IncludedColumns
                .Distinct()
                .Order()
                .Where(c => !table.NaturalKeyColumns.Contains(c))
                .Select(c => sourceColumns.First(sc => sc.ColumnName == c))
                .ToArray();
        }
        
        var missingColumnNames = string.Join(", ", missingColumns);
        throw new ScdTableValidationException(
            $"Schema drift was disabled and some included columns are missing from the source table: {missingColumnNames}");
    }
    
    private ScdColumnMetadata[] GetNonNkIncludedColumns(
        SchemaDriftEnabledConfiguration configuration,
        ScdColumnMetadata[] sourceColumns)
    {
        return sourceColumns
            .Where(c => !table.NaturalKeyColumns.Contains(c.ColumnName))
            .Where(c => !configuration.ExcludedColumns.Contains(c.ColumnName))
            .DistinctBy(c => c.ColumnName)
            .OrderBy(c => c.ColumnName)
            .ToArray();
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