using Biflow.Core.Entities.Scd;

namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record ScdTableDto(
    Guid ConnectionId,
    string ScdTableName,
    string SourceTableSchema,
    string SourceTableName,
    string TargetTableSchema,
    string TargetTableName,
    string? StagingTableSchema,
    string StagingTableName,
    string? PreLoadScript,
    string? PostLoadScript,
    bool FullLoad,
    bool ApplyIndexesOnCreate,
    bool SelectDistinct,
    string[] NaturalKeyColumns,
    SchemaDriftConfiguration SchemaDriftConfiguration);