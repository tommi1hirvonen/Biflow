namespace Biflow.Core.Entities.Scd;

public interface IColumn
{
    public string ColumnName { get; }
}

public interface IStructureColumn : IColumn
{
    public string DataType { get; }
    
    public bool IsNullable { get; }
}

public interface ILoadColumn : IColumn
{
    public bool IncludeInStagingTable { get; }
    
    public string? StagingTableExpression { get; }
    
    public string? TargetTableExpression { get; }
}

public interface IOrderedLoadColumn : ILoadColumn
{
    public int Ordinal { get; }
}

internal class StructureColumn : IStructureColumn
{
    public required string ColumnName { get; init; }
    
    public required string DataType { get; init; }
    
    public required bool IsNullable { get; init; }
}

internal class LoadColumn : ILoadColumn
{
    public required string ColumnName { get; init; }
    
    public required bool IncludeInStagingTable { get; init; }
    
    public required string? StagingTableExpression { get; init; }
    
    public required string? TargetTableExpression { get; init; }
}

public class FullColumnMetadata : IOrderedLoadColumn, IStructureColumn
{
    public required string ColumnName { get; init; }
    
    public required string DataType { get; init; }
    
    public required bool IsNullable { get; init; }
    
    public required int Ordinal { get; init; }
    
    public required bool IncludeInStagingTable { get; init; }
    
    public required string? StagingTableExpression { get; init; }
    
    public required string? TargetTableExpression { get; init; }
}