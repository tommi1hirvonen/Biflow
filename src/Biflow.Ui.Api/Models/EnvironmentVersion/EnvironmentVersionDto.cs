namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record EnvironmentVersionDto
{
    public required string Description { get; init; }
    
    public Guid? PropertyTranslationSetId { get; init; }
}