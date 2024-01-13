using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface IHasStepExecutionParameters<TParameter> : IHasStepExecutionParameters
    where TParameter : StepExecutionParameterBase
{
    public new IList<TParameter> StepExecutionParameters { get; }

    IList<StepExecutionParameterBase> IHasStepExecutionParameters.StepExecutionParameters =>
        StepExecutionParameters.Cast<StepExecutionParameterBase>().ToList();
}

public interface IHasStepExecutionParameters : IHasParameters
{
    public IList<StepExecutionParameterBase> StepExecutionParameters { get; }

    IList<ParameterBase> IHasParameters.Parameters => StepExecutionParameters.Cast<ParameterBase>().ToList();
}
