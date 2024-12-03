namespace Biflow.Core.Entities.Scd;

internal static class Scd
{
    public static IEnumerable<T> GetNonNkIncludedColumns<T>(
        IReadOnlyList<string> naturalKey, SchemaDriftDisabledConfiguration config, IReadOnlyList<T> columns)
        where T : IColumn
    {
        return config.IncludedColumns
            .Distinct()
            .Order()
            .Where(c => !naturalKey.Contains(c))
            .Select(c => columns.First(sc => sc.ColumnName == c));
    }

    public static IEnumerable<T> GetNonNkStructureIncludedColumns<T>(
        IReadOnlyList<string> naturalKey, SchemaDriftEnabledConfiguration config, IReadOnlyList<T> sourceColumns)
        where T : IColumn
    {
        return sourceColumns
            .Where(c => !naturalKey.Contains(c.ColumnName))
            .Where(c => !config.ExcludedColumns.Contains(c.ColumnName))
            .DistinctBy(c => c.ColumnName);
    }
    
    public static IEnumerable<T> GetNonNkLoadIncludedColumns<T>(
        IReadOnlyList<string> naturalKey,
        SchemaDriftEnabledConfiguration config,
        IReadOnlyList<T> sourceColumns,
        IReadOnlyList<T> targetColumns)
        where T : IColumn
    {
        var includedColumns = targetColumns
            .Where(c => !naturalKey.Contains(c.ColumnName))
            .Where(c => !config.ExcludedColumns.Contains(c.ColumnName))
            .DistinctBy(c => c.ColumnName)
            .ToArray();
        
        if (config.IgnoreMissingColumns)
        {
            return includedColumns
                .Where(c1 => sourceColumns.All(sc => sc.ColumnName != c1.ColumnName))
                .ToArray();            
        }

        var missingColumns = includedColumns
            .Where(c1 => sourceColumns.All(sc => sc.ColumnName != c1.ColumnName))
            .Select(c => c.ColumnName)
            .ToArray();
        
        if (missingColumns.Length <= 0)
        {
            return includedColumns;
        }
        
        var missingColumnNames = string.Join(", ", missingColumns);
        throw new ScdTableValidationException($"Schema drift table does not handle removed columns: {missingColumnNames}");
    }

    public static void EnsureScdTableValidated(ScdTable table, IColumn[] sourceColumns, IColumn[] targetColumns)
    {
        if (table.SourceTableSchema == table.TargetTableSchema && table.SourceTableName == table.TargetTableName)
        {
            throw new ScdTableValidationException("The source and target table cannot be the same");
        }
        
        if (table.SourceTableSchema == table.StagingTableSchema && table.SourceTableName == table.StagingTableName)
        {
            throw new ScdTableValidationException("The source and staging table cannot be the same");
        }
        
        if (table.StagingTableSchema == table.TargetTableSchema && table.StagingTableName == table.TargetTableName)
        {
            throw new ScdTableValidationException("The staging and target table cannot be the same");
        }

        if (table.NaturalKeyColumns.Count == 0)
        {
            throw new ScdTableValidationException("The table must have at least one natural key column.");
        }

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
                    var missingColumnNames = string.Join(", ", missingColumns.Select(c => c.ColumnName));
                    throw new ScdTableValidationException(
                        $"Schema drift enabled table is set to not handle removed columns and some columns are missing from the source: {missingColumnNames}");
                }

                break;
            }
        }
    }

    public static void EnsureScdTableValidatedForLoad(ScdTable table, IColumn[] targetColumns)
    {
        if (targetColumns.Length == 0)
        {
            throw new ScdTableValidationException("The target table does not exist");
        }
        
        var missingNkColumns = table.NaturalKeyColumns
            .Where(c => targetColumns.All(sc => sc.ColumnName != c))
            .ToArray();
        if (missingNkColumns.Length > 0)
        {
            var missingColumnNames = string.Join(", ", missingNkColumns);
            throw new ScdTableValidationException($"Natural key columns are missing from the target table: {missingColumnNames}");
        }

        if (table.SchemaDriftConfiguration is not SchemaDriftDisabledConfiguration disabled)
        {
            return;
        }
        
        var missingIncludedColumns = disabled.IncludedColumns
            .Where(c => targetColumns.All(sc => sc.ColumnName != c))
            .ToArray();
        
        if (missingIncludedColumns.Length <= 0)
        {
            return;
        }
        
        var missingIncludedColumnNames = string.Join(", ", missingIncludedColumns);
        throw new ScdTableValidationException($"Some included columns are missing from the target table: {missingIncludedColumnNames}");
    }
}