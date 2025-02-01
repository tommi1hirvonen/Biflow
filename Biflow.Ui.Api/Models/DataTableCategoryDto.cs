namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record DataTableCategoryDto
{
    public required string CategoryName { get; init; }
}