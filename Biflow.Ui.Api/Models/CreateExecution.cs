namespace Biflow.Ui.Api.Models;

[PublicAPI]
public record CreateExecution
{
    public required Guid JobId { get; init; }
    public Guid[]? StepIds { get; init; }

    public CreateExecutionJobParameterOverride[] JobParameterOverrides { get; init; } = [];
}

[PublicAPI]
public record CreateExecutionJobParameterOverride
{
    public required Guid ParameterId { get; init; }
    
    public ParameterValue ParameterValue { get; init; }
    
    public bool UseExpression  { get; init; }
    
    public string? Expression { get; init; }
}