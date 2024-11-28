namespace Biflow.Core.Entities.Scd;

public interface IScdProvider
{
    public Task<StagingLoadStatementResult> CreateStagingLoadStatementAsync(
        CancellationToken cancellationToken = default);
    
    public Task<string> CreateTargetLoadStatementAsync(CancellationToken cancellationToken = default);
    
    public string CreateTargetLoadStatement(
        IReadOnlyList<IOrderedLoadColumn> sourceColumns,
        IReadOnlyList<IOrderedLoadColumn> targetColumns);

    public Task<string> CreateStructureUpdateStatementAsync(CancellationToken cancellationToken = default);
}

public record StagingLoadStatementResult(
    string Statement,
    IReadOnlyList<IOrderedLoadColumn> SourceColumns,
    IReadOnlyList<IOrderedLoadColumn> TargetColumns);