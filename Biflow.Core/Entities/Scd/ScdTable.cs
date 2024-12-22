using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Biflow.Core.Entities.Scd;

namespace Biflow.Core.Entities;

public class ScdTable
{
    public Guid ScdTableId { get; init; }

    [Required, MaxLength(250)]
    public string ScdTableName { get; set; } = "";
    
    [Required, MaxLength(128)]
    public string SourceTableSchema { get; set; } = "";
    
    [Required, MaxLength(128)]
    public string SourceTableName { get; set; } = "";
    
    [Required, MaxLength(128)]
    public string TargetTableSchema { get; set; } = "";
    
    [Required, MaxLength(128)]
    public string TargetTableName { get; set; } = "";
    
    /// <summary>
    /// Optional staging table schema for the intermediary results.
    /// Leave blank to use default schema.
    /// </summary>
    [MaxLength(128)]
    public string? StagingTableSchema { get; set; }
    
    /// <summary>
    /// The name of the table to store intermediary results for merging incoming data from source to target.
    /// Temporary tables can be used if they are supported by the DBMS ('#' prefix on SQL Server).
    /// </summary>
    [Required, MaxLength(128)]
    public string StagingTableName { get; set; } = "";
    
    /// <summary>
    /// Script to be included in the load process before any data preparation or loads have been made.
    /// The script will be executed under the same transaction as the rest of the data load.
    /// Errors in pre-load script will roll back the entire transaction. 
    /// </summary>
    public string? PreLoadScript { get; set; }
    
    /// <summary>
    /// Script to be included in the load process after all data loads have been made.
    /// The script will be executed under the same transaction as the rest of the data load.
    /// Errors in post-load script will roll back the entire transaction.
    /// </summary>
    public string? PostLoadScript { get; set; }
    
    /// <summary>
    /// If full load is enabled, target table records that are missing in source table will be marked as invalid/expired.
    /// Missing records are identified based on their keys.
    /// </summary>
    public bool FullLoad { get; set; }

    /// <summary>
    /// Whether indexes should be created on the target table when it is first created.
    /// Note, that some SQL platforms may not support indexes.
    /// In those cases <see cref="ApplyIndexesOnCreate"/> should be set to <see langword="false" />.
    /// </summary>
    public bool ApplyIndexesOnCreate { get; set; } = true;
    
    /// <summary>
    /// Whether the select statement on the source table should be executed as distinct or not.
    /// Enabling <see cref="SelectDistinct"/> can help to get rid of potential duplicate records in source table
    /// but may have a negative performance impact with large data amounts.
    /// </summary>
    public bool SelectDistinct { get; set; }
    
    /// <summary>
    /// Columns that comprise the natural key for the source table.
    /// </summary>
    public List<string> NaturalKeyColumns { get; set; } = [];
    
    public SchemaDriftConfiguration SchemaDriftConfiguration { get; set; } = new SchemaDriftDisabledConfiguration();
    
    [Required]
    public Guid ConnectionId { get; set; }

    [JsonIgnore]
    public SqlConnectionBase Connection { get; init; } = null!;
    
    [JsonIgnore]
    public IEnumerable<ScdStep> ScdSteps { get; init; } = new List<ScdStep>();

    /// <summary>
    /// Removes a column from the list of natural key columns if it is found, otherwise adds it to the list.
    /// </summary>
    /// <param name="column">column to add/remove</param>
    public void ToggleNaturalKeyColumn(string column)
    {
        if (NaturalKeyColumns.Remove(column))
        {
            return;
        }
        NaturalKeyColumns.Add(column);
        switch (SchemaDriftConfiguration)
        {
            case SchemaDriftDisabledConfiguration disabled:
                disabled.IncludedColumns.RemoveAll(c => c == column);
                break;
            case SchemaDriftEnabledConfiguration enabled:
                enabled.ExcludedColumns.RemoveAll(c => c == column);
                break;
        }
    }

    public IScdProvider CreateScdProvider() => Connection.CreateScdProvider(this);
}