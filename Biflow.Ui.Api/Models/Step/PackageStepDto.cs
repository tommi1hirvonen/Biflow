namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public sealed record PackageStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required Guid ConnectionId { get; init; }
    public required string PackageFolderName { get; init; }
    public required string PackageProjectName { get; init; }
    public required string PackageName { get; init; } 
    public required bool ExecuteIn32BitMode { get; init; }
    public required PackageStepParameterDto[] Parameters { get; init; }
    public string? ExecuteAsLogin { get; init; }
}