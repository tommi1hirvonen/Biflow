using System.Text;

namespace Biflow.Core.Entities.Scd;

internal abstract class ScdProvider<TSyntaxProvider>(
    ScdTable table, IColumnMetadataProvider columnProvider) : IScdProvider
    where TSyntaxProvider : ISqlSyntaxProvider
{
    protected abstract string HashKeyColumn { get; }
    
    protected abstract string ValidFromColumn { get; }
    
    protected abstract string ValidUntilColumn { get; }
    
    protected abstract string IsCurrentColumn { get; }
    
    protected abstract string RecordHashColumn { get; }

    private IEnumerable<string> SystemColumns =>
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
        var sourceTableName =
            $"{TSyntaxProvider.QuoteName(table.SourceTableSchema)}.{TSyntaxProvider.QuoteName(table.SourceTableName)}";
        var stagingTableName = string.IsNullOrEmpty(table.StagingTableSchema)
            ? TSyntaxProvider.QuoteName(table.StagingTableName)
            : $"{TSyntaxProvider.QuoteName(table.StagingTableSchema)}.{TSyntaxProvider.QuoteName(table.StagingTableName)}";
        var select = includedColumns
            .Where(c => c.IncludeInStagingTable)
            .Select(c => (c.StagingTableExpression ?? c.ColumnName, c.ColumnName));
        var ctas = TSyntaxProvider.Ctas(sourceTableName, stagingTableName, select, table.SelectDistinct);
        var statement = TSyntaxProvider.SupportsDdlRollback
            ? TSyntaxProvider.RollbackOnError(ctas)
            : TSyntaxProvider.WithBlock(ctas);
        
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
        
        var includedColumns = GetDataLoadColumns(sourceColumns, targetColumns);
        
        var targetTableName =
            $"{TSyntaxProvider.QuoteName(table.TargetTableSchema)}.{TSyntaxProvider.QuoteName(table.TargetTableName)}";
        var stagingTableName = string.IsNullOrEmpty(table.StagingTableSchema)
            ? TSyntaxProvider.QuoteName(table.StagingTableName)
            : $"{TSyntaxProvider.QuoteName(table.StagingTableSchema)}.{TSyntaxProvider.QuoteName(table.StagingTableName)}";
        var quotedInsertColumns = includedColumns
            .Select(c => TSyntaxProvider.QuoteName(c.ColumnName));
        var quotedSelectColumns = includedColumns
            .Select(c => c.TargetTableExpression is null
                ? $"src.{TSyntaxProvider.QuoteName(c.ColumnName)}"
                : $"{c.TargetTableExpression} AS {TSyntaxProvider.QuoteName(c.ColumnName)}");

        var update = TSyntaxProvider.ScdUpdate(
            source: stagingTableName,
            target: targetTableName,
            fullLoad: table.FullLoad,
            isCurrentColumn: TSyntaxProvider.QuoteName(IsCurrentColumn),
            validUntilColumn: TSyntaxProvider.QuoteName(ValidUntilColumn),
            hashKeyColumn: TSyntaxProvider.QuoteName(HashKeyColumn),
            recordHashColumn: TSyntaxProvider.QuoteName(RecordHashColumn));

        var insert = $"""
            INSERT INTO {targetTableName} (
            {string.Join(",\n", quotedInsertColumns)}
            )
            SELECT
            {string.Join(",\n", quotedSelectColumns)}
            FROM {stagingTableName} AS src
              LEFT JOIN {targetTableName} AS tgt ON
                  src.{TSyntaxProvider.QuoteName(HashKeyColumn)} = tgt.{TSyntaxProvider.QuoteName(HashKeyColumn)} AND
                  tgt.{TSyntaxProvider.QuoteName(IsCurrentColumn)} = 1
            WHERE tgt.{TSyntaxProvider.QuoteName(HashKeyColumn)} IS NULL OR 
                src.{TSyntaxProvider.QuoteName(RecordHashColumn)} <> tgt.{TSyntaxProvider.QuoteName(RecordHashColumn)};
                
            """;
        
        var block = TSyntaxProvider.RollbackOnError($"""

            {table.PreLoadScript}

            {update}

            {insert}

            {table.PostLoadScript}

            """);
        
        return block;
    }

    public async Task<string> CreateStructureUpdateStatementAsync(CancellationToken cancellationToken = default)
    {
        var (sourceColumns, targetColumns) = await GetSourceTargetColumnsAsync<IStructureColumn>(cancellationToken);

        Scd.EnsureScdTableValidated(
            table,
            sourceColumns.Cast<IColumn>().ToArray(),
            targetColumns.Cast<IColumn>().ToArray());
        
        var builder = new StringBuilder();
        
        var tableName =
            $"{TSyntaxProvider.QuoteName(table.TargetTableSchema)}.{TSyntaxProvider.QuoteName(table.TargetTableName)}";

        if (targetColumns.Count == 0)
        {
            // Target table does not exist.
            var columnDefinitions = GetStructureColumns(sourceColumns)
                .Select(c => $"{TSyntaxProvider.QuoteName(c.ColumnName)} {c.DataType} {(c.IsNullable ? "NULL" : "NOT NULL")}");
            
            builder.AppendLine($"""
                CREATE TABLE {tableName} (
                {string.Join(",\n", columnDefinitions)}
                );

                """);

            if (!table.ApplyIndexesOnCreate || !TSyntaxProvider.SupportsIndexes)
            {
                return TSyntaxProvider.WithBlock(builder.ToString());
            }
            
            var nkIndexName = TSyntaxProvider.QuoteName($"IX_{table.TargetTableName}_NaturalKey");
            var nkIndexColumnDefinitions = table.NaturalKeyColumns
                .Distinct()
                .Order()
                .Select(c => $"{TSyntaxProvider.QuoteName(c)} ASC")
                .Append($"{TSyntaxProvider.QuoteName(IsCurrentColumn)} ASC");
            var hkIndexName = TSyntaxProvider.QuoteName($"IX_{table.TargetTableName}_HashKey");
            var hkIndexColumnDefinition =
                $"{TSyntaxProvider.QuoteName(HashKeyColumn)} ASC, {TSyntaxProvider.QuoteName(IsCurrentColumn)} ASC";
            builder.AppendLine($"""
                CREATE CLUSTERED INDEX {hkIndexName} ON {tableName} (
                {hkIndexColumnDefinition}
                );

                CREATE NONCLUSTERED INDEX {nkIndexName} ON {tableName} (
                {string.Join(",\n", nkIndexColumnDefinitions)}
                );

                """);
            
            return TSyntaxProvider.WithBlock(builder.ToString());
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

        if (columnsToAdd.Length == 0 && columnsToAlter.Length == 0)
        {
            return "";
        }

        foreach (var c in columnsToAlter)
        {
            builder.AppendLine(TSyntaxProvider.AlterColumnDropNull(tableName, c));
        }

        foreach (var c in columnsToAdd.OrderBy(c => c.ColumnName))
        {
            // New columns are nullable because target table might already contain data
            builder.AppendLine(TSyntaxProvider.AlterTableAddColumn(tableName, c, nullable: true));
        }
        
        return TSyntaxProvider.WithBlock(builder.ToString());
    }

    private IReadOnlyList<IStructureColumn> GetStructureColumns(IReadOnlyList<IStructureColumn> sourceColumns)
    {
        var hashKeyColumn = new StructureColumn
        {
            ColumnName = HashKeyColumn,
            DataType = TSyntaxProvider.Varchar(32),
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
            DataType = TSyntaxProvider.DateTime,
            IsNullable = false
        };
        var validUntil = new StructureColumn
        {
            ColumnName = ValidUntilColumn,
            DataType = TSyntaxProvider.DateTime,
            IsNullable = true
        };
        var isCurrent = new StructureColumn
        {
            ColumnName = IsCurrentColumn,
            DataType = TSyntaxProvider.Boolean,
            IsNullable = false
        };
        
        var recordHashColumn = new StructureColumn
        {
            ColumnName = RecordHashColumn,
            DataType = TSyntaxProvider.Varchar(32),
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
            .Select(TSyntaxProvider.QuoteName)
            .ToArray();
        var hashKeyColumn = new LoadColumn
        {
            ColumnName = HashKeyColumn,
            IncludeInStagingTable = true,
            StagingTableExpression = TSyntaxProvider.Md5(quotedNkColumns),
            TargetTableExpression = null
        };
        var validFrom = new LoadColumn
        {
            ColumnName = ValidFromColumn,
            IncludeInStagingTable = false,
            StagingTableExpression = null,
            TargetTableExpression = TSyntaxProvider.CurrentTimestamp
        };
        var validUntil = new LoadColumn
        {
            ColumnName = ValidUntilColumn,
            IncludeInStagingTable = false,
            StagingTableExpression = null,
            TargetTableExpression = TSyntaxProvider.MaxDateTime
        };
        var isCurrent = new LoadColumn
        {
            ColumnName = IsCurrentColumn,
            IncludeInStagingTable = false,
            StagingTableExpression = null,
            TargetTableExpression = TSyntaxProvider.True
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
            .Select(c => c.ColumnName)
            .Select(TSyntaxProvider.QuoteName);
        var recordHashColumn = new LoadColumn
        {
            ColumnName = RecordHashColumn,
            IncludeInStagingTable = true,
            // MD5 is the most reliable way of case-sensitively capturing all changes.
            StagingTableExpression = TSyntaxProvider.Md5(quotedRecordColumns),
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