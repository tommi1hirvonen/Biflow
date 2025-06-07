namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record DataTableLookupDto
{
    public Guid? LookupId { get; init; }
    public required string ColumnName { get; init; }
    public required Guid LookupDataTableId { get; init; }
    public required string LookupValueColumn { get; init; }
    public required string LookupDescriptionColumn { get; init; }
    public required LookupDisplayType LookupDisplayType { get; init; }
}