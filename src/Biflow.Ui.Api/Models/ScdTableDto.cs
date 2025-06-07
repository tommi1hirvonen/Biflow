using Biflow.Core.Entities.Scd;

namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record ScdTableDto
{
    public required Guid ConnectionId { get; init; }
    public required string ScdTableName { get; init; }
    public required string SourceTableSchema { get; init; }
    public required string SourceTableName { get; init; }
    public required string TargetTableSchema { get; init; }
    public required string TargetTableName { get; init; }
    public required string? StagingTableSchema { get; init; }
    public required string StagingTableName { get; init; }
    public string? PreLoadScript { get; init; }
    public string? PostLoadScript { get; init; }
    public required bool FullLoad { get; init; }
    public required bool ApplyIndexesOnCreate { get; init; }
    public required bool SelectDistinct { get; init; }
    public required string[] NaturalKeyColumns { get; init; }
    public required SchemaDriftConfiguration SchemaDriftConfiguration { get; init; }
}