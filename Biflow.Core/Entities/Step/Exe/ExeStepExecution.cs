using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class ExeStepExecution : StepExecution, IHasTimeout, IHasStepExecutionParameters<ExeStepExecutionParameter>
{
    public ExeStepExecution(string stepName, string exeFileName) : base(stepName, StepType.Exe)
    {
        ExeFileName = exeFileName;
    }

    public ExeStepExecution(ExeStep step, Execution execution) : base(step, execution)
    {
        ArgumentNullException.ThrowIfNull(step.ExeFileName);

        ExeFileName = step.ExeFileName;
        ExeArguments = step.ExeArguments;
        ExeWorkingDirectory = step.ExeWorkingDirectory;
        ExeSuccessExitCode = step.ExeSuccessExitCode;
        TimeoutMinutes = step.TimeoutMinutes;

        StepExecutionParameters = step.StepParameters
            .Select(p => new ExeStepExecutionParameter(p, this))
            .ToArray();
        StepExecutionAttempts.Add(new ExeStepExecutionAttempt(this));
    }

    [Display(Name = "File path")]
    [MaxLength(1000)]
    public string ExeFileName { get; private set; }

    [Display(Name = "Arguments")]
    public string? ExeArguments { get; private set; }

    [Display(Name = "Working directory")]
    [MaxLength(1000)]
    public string? ExeWorkingDirectory { get; private set; }

    [Display(Name = "Success exit code")]
    public int? ExeSuccessExitCode { get; private set; }

    public double TimeoutMinutes { get; private set; }

    public IList<ExeStepExecutionParameter> StepExecutionParameters { get; set; } = null!;
}
