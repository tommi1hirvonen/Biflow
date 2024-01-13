using Biflow.Core.Entities;

namespace Biflow.Core.Interfaces;

public interface IHasStepParameters<TParameter> : IHasStepParameters
    where TParameter : StepParameterBase
{
    public new IList<TParameter> StepParameters { get; }

    IList<StepParameterBase> IHasStepParameters.StepParameters => StepParameters.Cast<StepParameterBase>().ToList();
}

public interface IHasStepParameters : IHasParameters
{
    public IList<StepParameterBase> StepParameters { get; }

    IList<ParameterBase> IHasParameters.Parameters => StepParameters.Cast<ParameterBase>().ToList();
}
