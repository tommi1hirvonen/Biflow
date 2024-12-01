using System.Text;

namespace Biflow.Core.Entities.Scd;

internal abstract class ScdProvider<TSyntaxProvider>(
    ScdTable table, IColumnMetadataProvider columnProvider) : IScdProvider
    where TSyntaxProvider : ISqlSyntaxProvider, new()
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
    
    private TSyntaxProvider SyntaxProvider { get; } = new();
    
    public async Task<StagingLoadStatementResult> CreateStagingLoadStatementAsync(
        CancellationToken cancellationToken = default)
    {
        var (sourceColumns, targetColumns) = await GetSourceTargetColumnsAsync<IOrderedLoadColumn>(cancellationToken);
        
        Scd.EnsureScdTableValidated(
            table,
            sourceColumns.Cast<IColumn>().ToArray(),
            targetColumns.Cast<IColumn>().ToArray());
        Scd.EnsureScdTableValidatedForLoad(table, targetColumns.Cast<IColumn>().ToArray());
        
        var includedColumns = GetDataLoadColumns(sourceColumns, targetColumns);
        var select = includedColumns
            .Where(c => c.IncludeInStagingTable)
            .Select(c => (c.StagingTableExpression, c.ColumnName));
        var ctas = SyntaxProvider.Ctas(
            table.SourceTableSchema,
            table.SourceTableName,
            table.StagingTableSchema,
            table.StagingTableName,
            select,
            table.SelectDistinct);
        var statement = SyntaxProvider.SupportsDdlRollback
            ? SyntaxProvider.RollbackOnError(ctas)
            : SyntaxProvider.WithBlock(ctas);
        
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
        Scd.EnsureScdTableValidatedForLoad(table, targetColumns.Cast<IColumn>().ToArray());

        var update = SyntaxProvider.ScdUpdate(
            sourceSchema: table.StagingTableSchema,
            sourceTable: table.StagingTableName,
            targetSchema: table.TargetTableSchema,
            targetTable: table.TargetTableName,
            fullLoad: table.FullLoad,
            isCurrentColumn: IsCurrentColumn,
            validUntilColumn: ValidUntilColumn,
            hashKeyColumn: HashKeyColumn,
            recordHashColumn: RecordHashColumn);
        
        var targetTableName = SyntaxProvider.QuoteTable(table.TargetTableSchema, table.TargetTableName);
        var stagingTableName = SyntaxProvider.QuoteTable(table.StagingTableSchema, table.StagingTableName);
        
        var includedColumns = GetDataLoadColumns(sourceColumns, targetColumns);
        var quotedInsertColumns = includedColumns
            .Select(c => SyntaxProvider.QuoteColumn(c.ColumnName));
        var quotedSelectColumns = includedColumns
            .Select(c => c.TargetTableExpression is null
                ? $"src.{SyntaxProvider.QuoteColumn(c.ColumnName)}"
                : $"{c.TargetTableExpression} AS {SyntaxProvider.QuoteColumn(c.ColumnName)}");

        var insert = $"""
            INSERT INTO {targetTableName} (
            {string.Join(",\n", quotedInsertColumns)}
            )
            SELECT
            {string.Join(",\n", quotedSelectColumns)}
            FROM {stagingTableName} AS src
              LEFT JOIN {targetTableName} AS tgt ON
                  src.{SyntaxProvider.QuoteColumn(HashKeyColumn)} = tgt.{SyntaxProvider.QuoteColumn(HashKeyColumn)} AND
                  tgt.{SyntaxProvider.QuoteColumn(IsCurrentColumn)} = 1
            WHERE tgt.{SyntaxProvider.QuoteColumn(HashKeyColumn)} IS NULL OR 
                src.{SyntaxProvider.QuoteColumn(RecordHashColumn)} <> tgt.{SyntaxProvider.QuoteColumn(RecordHashColumn)};
            """;
        
        var block = SyntaxProvider.RollbackOnError($"""
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

        if (targetColumns.Count == 0)
        {
            // Target table does not exist.
            var columns = GetStructureColumns(sourceColumns);
            builder.AppendLine(SyntaxProvider.CreateTable(table.TargetTableSchema, table.TargetTableName, columns));

            if (!table.ApplyIndexesOnCreate || !SyntaxProvider.Indexes.AreSupported)
            {
                return SyntaxProvider.WithBlock(builder.ToString());
            }
            
            var naturalKeyIndexName = $"IX_{table.TargetTableName}_NaturalKey";
            var naturalKeyIndexColumns = table.NaturalKeyColumns
                .Distinct()
                .Order()
                .Append(IsCurrentColumn)
                .Select(c => (ColumnName: c, Descending: false));
            
            builder.AppendLine(SyntaxProvider.Indexes.ClusteredIndex(
                table.TargetTableSchema, table.TargetTableName, naturalKeyIndexName, naturalKeyIndexColumns));
            
            var hashKeyIndexName = $"IX_{table.TargetTableName}_HashKey";
            var hashKeyColumns = Enumerable.Empty<string>()
                .Append(HashKeyColumn)
                .Append(IsCurrentColumn)
                .Select(c => (ColumnName: c, Descending: false));
            
            builder.AppendLine(SyntaxProvider.Indexes.NonClusteredIndex(
                table.TargetTableSchema, table.TargetTableName, hashKeyIndexName, hashKeyColumns));
            
            return SyntaxProvider.WithBlock(builder.ToString());
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
                // Newly included columns. Also account for new columns via additions to natural key.
                columnsToAdd = sourceColumns
                    .Where(c => disabled.IncludedColumns.Contains(c.ColumnName)
                                || table.NaturalKeyColumns.Contains(c.ColumnName))
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
                        .Where(c => !enabled.ExcludedColumns.Contains(c.ColumnName)
                                    || table.NaturalKeyColumns.Contains(c.ColumnName))
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
            builder.AppendLine(SyntaxProvider.AlterColumnDropNull(table.TargetTableSchema, table.TargetTableName, c));
        }

        foreach (var c in columnsToAdd.OrderBy(c => c.ColumnName))
        {
            // New columns are nullable because target table might already contain data
            builder.AppendLine(SyntaxProvider.AlterTableAddColumn(
                table.TargetTableSchema, table.TargetTableName, c, nullable: true));
        }
        
        return SyntaxProvider.WithBlock(builder.ToString());
    }

    private IReadOnlyList<IStructureColumn> GetStructureColumns(IReadOnlyList<IStructureColumn> sourceColumns)
    {
        var hashKeyColumn = new StructureColumn
        {
            ColumnName = HashKeyColumn,
            DataType = SyntaxProvider.Datatypes.Varchar(32),
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
            DataType = SyntaxProvider.Datatypes.DateTime,
            IsNullable = false
        };
        var validUntil = new StructureColumn
        {
            ColumnName = ValidUntilColumn,
            DataType = SyntaxProvider.Datatypes.DateTime,
            IsNullable = false
        };
        var isCurrent = new StructureColumn
        {
            ColumnName = IsCurrentColumn,
            DataType = SyntaxProvider.Datatypes.Boolean,
            IsNullable = false
        };
        
        var recordHashColumn = new StructureColumn
        {
            ColumnName = RecordHashColumn,
            DataType = SyntaxProvider.Datatypes.Varchar(32),
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
            .ToArray();
        var hashKeyColumn = new LoadColumn
        {
            ColumnName = HashKeyColumn,
            IncludeInStagingTable = true,
            StagingTableExpression = SyntaxProvider.Functions.Md5(quotedNkColumns),
            TargetTableExpression = null
        };
        var validFrom = new LoadColumn
        {
            ColumnName = ValidFromColumn,
            IncludeInStagingTable = false,
            StagingTableExpression = null,
            TargetTableExpression = SyntaxProvider.Functions.CurrentTimestamp
        };
        var validUntil = new LoadColumn
        {
            ColumnName = ValidUntilColumn,
            IncludeInStagingTable = false,
            StagingTableExpression = null,
            TargetTableExpression = SyntaxProvider.Functions.MaxDateTime
        };
        var isCurrent = new LoadColumn
        {
            ColumnName = IsCurrentColumn,
            IncludeInStagingTable = false,
            StagingTableExpression = null,
            TargetTableExpression = SyntaxProvider.Functions.True
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
            .Select(c => c.ColumnName);
        var recordHashColumn = new LoadColumn
        {
            ColumnName = RecordHashColumn,
            IncludeInStagingTable = true,
            // MD5 is the most reliable way of case-sensitively capturing all changes.
            StagingTableExpression = SyntaxProvider.Functions.Md5(quotedRecordColumns),
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