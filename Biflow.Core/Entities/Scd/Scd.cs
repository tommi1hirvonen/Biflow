namespace Biflow.Core.Entities.Scd;

internal static class Scd
{
    public static IEnumerable<T> GetNonNkIncludedColumns<T>(
        IReadOnlyList<string> naturalKey, SchemaDriftDisabledConfiguration config, T[] columns)
        where T : IColumn
    {
        var missingColumns = config.IncludedColumns
            .Where(c => columns.All(sc => sc.ColumnName != c))
            .ToArray();
        
        if (missingColumns.Length <= 0)
        {
            return config.IncludedColumns
                .Distinct()
                .Order()
                .Where(c => !naturalKey.Contains(c))
                .Select(c => columns.First(sc => sc.ColumnName == c));
        }
        
        var missingColumnNames = string.Join(", ", missingColumns);
        throw new ScdTableValidationException(
            $"Schema drift was disabled and some included columns are missing from the table: {missingColumnNames}");
    }

    public static IEnumerable<T> GetNonNkStructureIncludedColumns<T>(
        IReadOnlyList<string> naturalKey, SchemaDriftEnabledConfiguration config, T[] sourceColumns)
        where T : IColumn
    {
        return sourceColumns
            .Where(c => !naturalKey.Contains(c.ColumnName))
            .Where(c => !config.ExcludedColumns.Contains(c.ColumnName))
            .DistinctBy(c => c.ColumnName);
    }
    
    public static IEnumerable<T> GetNonNkLoadIncludedColumns<T>(
        IReadOnlyList<string> naturalKey, SchemaDriftEnabledConfiguration config, T[] sourceColumns, T[] targetColumns)
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
}