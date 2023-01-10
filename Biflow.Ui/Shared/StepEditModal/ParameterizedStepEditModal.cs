using Biflow.DataAccess.Models;

namespace Biflow.Ui.Shared.StepEditModal;

public abstract class ParameterizedStepEditModal<TStep> : StepEditModalBase<TStep> where TStep : ParameterizedStep
{
    protected override (bool Result, string? ErrorMessage) StepValidityCheck(Step step)
    {
        (var paramResultBase, var paramMessageBase) = base.StepValidityCheck(step);
        if (!paramResultBase)
        {
            return (false, paramMessageBase);
        }
        if (step is ParameterizedStep step_)
        {
            (var paramResult, var paramMessage) = ParametersCheck();
            if (!paramResult)
            {
                return (false, paramMessage);
            }
            else
            {
                foreach (var param in step_.StepParameters)
                {
                    param.SetParameterValue();
                }
                return (true, null);
            }
        }
        else
        {
            return (false, "Not ParameterizedStep");
        }
    }

    private (bool Result, string? Message) ParametersCheck()
    {
        ArgumentNullException.ThrowIfNull(Step);
        var parameters = Step.StepParameters.OrderBy(param => param.ParameterName).ToList();
        foreach (var param in parameters)
        {
            if (string.IsNullOrEmpty(param.ParameterName))
            {
                return (false, "Parameter name cannot be empty");
            }
        }
        for (var i = 0; i < parameters.Count - 1; i++)
        {
            if (parameters[i + 1].ParameterName == parameters[i].ParameterName)
            {
                return (false, "Duplicate parameter names");
            }
        }

        return (true, null);
    }

}
