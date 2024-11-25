using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

[JsonDerivedType(typeof(SchemaDriftDisabledConfiguration), "SchemaDriftDisabled")]
[JsonDerivedType(typeof(SchemaDriftEnabledConfiguration), "SchemaDriftEnabled")]
public abstract class SchemaDriftConfiguration;

public sealed class SchemaDriftDisabledConfiguration : SchemaDriftConfiguration
{
    /// <summary>
    /// If schema drift is disabled, included columns need to be explicitly defined.
    /// Included columns not found in the source table will cause a validation exception when load is executed.
    /// </summary>
    public List<string> IncludedColumns { get; set; } = [];
    
    /// <summary>
    /// Removes a column from the list of included columns if it is found, otherwise adds it to the list.
    /// </summary>
    /// <param name="column">column to add/remove</param>
    public void ToggleIncludedColumn(string column)
    {
        if (IncludedColumns.Remove(column))
        {
            return;
        }
        IncludedColumns.Add(column);
    }
}

public sealed class SchemaDriftEnabledConfiguration : SchemaDriftConfiguration
{
    /// <summary>
    /// Automatically include new non-excluded columns from the source table.
    /// Default is true
    /// </summary>
    public bool IncludeNewColumns { get; set; } = true;
    
    /// <summary>
    /// Silently ignore removed source table columns.
    /// Default is false
    /// </summary>
    public bool IgnoreMissingColumns { get; set; }
    
    /// <summary>
    /// If schema drift is enabled, specific columns can be explicitly excluded.
    /// </summary>
    public List<string> ExcludedColumns { get; init; } = [];
    
    /// <summary>
    /// Removes a column from the list of excluded columns if it is found, otherwise adds it to the list.
    /// </summary>
    /// <param name="column">column to add/remove</param>
    public void ToggleExcludedColumn(string column)
    {
        if (ExcludedColumns.Remove(column))
        {
            return;
        }
        ExcludedColumns.Add(column);
    }
}