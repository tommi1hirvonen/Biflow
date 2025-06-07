namespace Biflow.Ui.Api.Models.Integrations;

[PublicAPI]
public record UpdateSnowflakeConnection
{
    public required string ConnectionName { get; init; }
    public required int MaxConcurrentSqlSteps { get; init; }
    public string? ScdDefaultTargetSchema  { get; init; }
    public string? ScdDefaultTargetTableSuffix  { get; init; }
    public string? ScdDefaultStagingSchema  { get; init; }
    public string? ScdDefaultStagingTableSuffix  { get; init; }
    public string? ConnectionString { get; init; }
}