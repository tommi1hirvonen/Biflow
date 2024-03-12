using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class ExeStepExecution : StepExecution,
    IHasTimeout,
    IHasStepExecutionParameters<ExeStepExecutionParameter>,
    IHasStepExecutionAttempts<ExeStepExecutionAttempt>
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
        Domain = step.Domain;
        Username = step.Username;
        Password = step.Password;

        StepExecutionParameters = step.StepParameters
            .Select(p => new ExeStepExecutionParameter(p, this))
            .ToArray();
        AddAttempt(new ExeStepExecutionAttempt(this));
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

    public string? Domain { get; private set; }

    public string? Username { get; private set; }

    public string? Password { get; private set; }

    public IEnumerable<ExeStepExecutionParameter> StepExecutionParameters { get; } = new List<ExeStepExecutionParameter>();

    public override ExeStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new ExeStepExecutionAttempt((ExeStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
        {
            ExecutionStatus = withStatus
        };
        AddAttempt(next);
        return next;
    }
}
