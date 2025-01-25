namespace Biflow.Ui.Api.Models.Step;

[PublicAPI]
public record ExecutionConditionParameterDto(
    Guid? ParameterId,
    string ParameterName,
    ParameterValue ParameterValue,
    Guid? InheritFromJobParameterId);