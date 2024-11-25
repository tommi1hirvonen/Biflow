namespace Biflow.Core.Entities;

public class ScdColumnMetadata
{
    public required string ColumnName { get; init; }
    
    public required string DataType { get; init; }
    
    public required bool IsNullable { get; init; }
    
    public required bool IncludeInStagingTable { get; init; }
    
    public required string? StagingTableExpression { get; init; }
    
    public required string? TargetTableExpression { get; init; }
}