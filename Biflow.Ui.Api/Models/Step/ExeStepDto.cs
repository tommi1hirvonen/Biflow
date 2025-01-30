namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record ExeStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required string FilePath { get; init; }
    public required string? Arguments { get; init; }
    public required string? WorkingDirectory { get; init; }
    public required int? SuccessExitCode { get; init; }
    public required Guid? RunAsCredentialId { get; init; }
    public required StepParameterDto[] Parameters { get; init; }
}