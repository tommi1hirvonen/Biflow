namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record VmStepDto : StepDto
{
    public required Guid AzureCredentialId { get; init; }
    public required string VirtualMachineResourceId { get; init; }
    public required VmStepOperation Operation { get; init; }
}
