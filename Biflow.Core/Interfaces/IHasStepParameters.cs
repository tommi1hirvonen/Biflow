using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface IHasStepParameters<TParameter> : IHasStepParameters
    where TParameter : StepParameterBase
{
    public new IList<TParameter> StepParameters { get; }

    IEnumerable<StepParameterBase> IHasStepParameters.StepParameters => StepParameters.Cast<StepParameterBase>();
}

public interface IHasStepParameters : IHasParameters
{
    public IEnumerable<StepParameterBase> StepParameters { get; }

    IEnumerable<ParameterBase> IHasParameters.Parameters => StepParameters.Cast<ParameterBase>();
}
