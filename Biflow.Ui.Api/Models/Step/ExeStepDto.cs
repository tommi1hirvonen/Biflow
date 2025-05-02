namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record ExeStepDto : StepDto
{
    public required double TimeoutMinutes { get; init; }
    public required string FilePath { get; init; }
    public string? Arguments { get; init; }
    public string? WorkingDirectory { get; init; }
    public int? SuccessExitCode { get; init; }
    public Guid? RunAsCredentialId { get; init; }
    public Guid? ProxyId { get; init; }
    public StepParameterDto[] Parameters { get; init; } = [];
}