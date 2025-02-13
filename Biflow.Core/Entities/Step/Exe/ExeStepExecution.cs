using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

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
        RunAsCredentialId = step.RunAsCredentialId;
        RunAsUsername = step.RunAsCredential?.DisplayName;

        StepExecutionParameters = step.StepParameters
            .Select(p => new ExeStepExecutionParameter(p, this))
            .ToArray();
        AddAttempt(new ExeStepExecutionAttempt(this));
    }

    [MaxLength(1000)]
    public string ExeFileName { get; private set; }

    public string? ExeArguments { get; private set; }

    [MaxLength(1000)]
    public string? ExeWorkingDirectory { get; private set; }

    public int? ExeSuccessExitCode { get; private set; }

    public double TimeoutMinutes { get; [UsedImplicitly] private set; }

    public Guid? RunAsCredentialId { get; [UsedImplicitly] private set; }

    public string? RunAsUsername { get; private set; }

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

    /// <summary>
    /// Get the <see cref="Credential"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetRunAsCredential(Credential?)"/> will need to have been called first for the <see cref="Credential"/> to be available.
    /// </summary>
    /// <returns><see cref="Credential"/> if it was previously set using <see cref="SetRunAsCredential(Credential?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public Credential? GetRunAsCredential() => _runAsCredential;

    /// <summary>
    /// Set the private <see cref="Credential"/> object used for defining the possible run-as credential.
    /// It can be later accessed using <see cref="GetRunAsCredential"/>.
    /// </summary>
    /// <param name="credential"><see cref="Credential"/> reference to store.
    /// The CredentialIds are compared and the value is set only if the ids match.</param>
    public void SetRunAsCredential(Credential? credential)
    {
        if (credential?.CredentialId == RunAsCredentialId)
        {
            _runAsCredential = credential;
        }
    }

    // Use a field excluded from the EF model to store the credential reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    private Credential? _runAsCredential;
}
