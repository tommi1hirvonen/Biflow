namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record DataTableDto
{
    public required string DataTableName { get; init; }
    public string? DataTableDescription { get; init; }
    public required string TargetSchemaName { get; init; }
    public required string TargetTableName { get; init; }
    public required Guid ConnectionId { get; init; }
    public Guid? CategoryId { get; init; }
    public required bool AllowInsert { get; init; }
    public required bool AllowDelete { get; init; }
    public required bool AllowUpdate { get; init; }
    public required bool AllowImport { get; init; }
    public required int DefaultEditorRowLimit { get; init; }
    public required string[] LockedColumns { get; init; }
    public required bool LockedColumnsExcludeMode { get; init; }
    public required string[] HiddenColumns { get; init; }
    public required string[] ColumnOrder { get; init; }
    public required DataTableLookupDto[] Lookups { get; init; }
}