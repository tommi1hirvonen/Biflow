using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface IHasStepExecutionParameters<out TParameter> : IHasStepExecutionParameters
    where TParameter : StepExecutionParameterBase
{
    public new IEnumerable<TParameter> StepExecutionParameters { get; }

    IEnumerable<StepExecutionParameterBase> IHasStepExecutionParameters.StepExecutionParameters =>
        StepExecutionParameters;
}

public interface IHasStepExecutionParameters : IHasParameters
{
    public IEnumerable<StepExecutionParameterBase> StepExecutionParameters { get; }

    IEnumerable<ParameterBase> IHasParameters.Parameters => StepExecutionParameters;
}
