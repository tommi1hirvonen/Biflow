namespace Biflow.Core.Entities.Scd;

public record StagingLoadStatementResult(
    string Statement,
    IReadOnlyList<IOrderedLoadColumn> SourceColumns,
    IReadOnlyList<IOrderedLoadColumn> TargetColumns);