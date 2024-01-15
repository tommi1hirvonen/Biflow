using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface IHasStepExecutionParameters<TParameter> : IHasStepExecutionParameters
    where TParameter : StepExecutionParameterBase
{
    public new IEnumerable<TParameter> StepExecutionParameters { get; }

    IEnumerable<StepExecutionParameterBase> IHasStepExecutionParameters.StepExecutionParameters =>
        StepExecutionParameters.Cast<StepExecutionParameterBase>();
}

public interface IHasStepExecutionParameters : IHasParameters
{
    public IEnumerable<StepExecutionParameterBase> StepExecutionParameters { get; }

    IEnumerable<ParameterBase> IHasParameters.Parameters => StepExecutionParameters.Cast<ParameterBase>();
}
