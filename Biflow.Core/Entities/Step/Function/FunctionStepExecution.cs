﻿using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Biflow.Core.Entities;

public class FunctionStepExecution : StepExecution,
    IHasTimeout,
    IHasStepExecutionParameters<FunctionStepExecutionParameter>,
    IHasStepExecutionAttempts<FunctionStepExecutionAttempt>
{
    public FunctionStepExecution(string stepName, Guid? functionAppId, string functionUrl) : base(stepName, StepType.Function)
    {
        FunctionAppId = functionAppId;
        FunctionUrl = functionUrl;
    }

    public FunctionStepExecution(FunctionStep step, Execution execution) : base(step, execution)
    {
        FunctionAppId = step.FunctionAppId;
        FunctionUrl = step.FunctionUrl;
        FunctionInput = step.FunctionInput;
        FunctionIsDurable = step.FunctionIsDurable;
        TimeoutMinutes = step.TimeoutMinutes;

        StepExecutionParameters = step.StepParameters
            .Select(p => new FunctionStepExecutionParameter(p, this))
            .ToArray();
        AddAttempt(new FunctionStepExecutionAttempt(this));
    }

    public Guid? FunctionAppId { get; [UsedImplicitly] private set; }

    [MaxLength(1000)]
    public string FunctionUrl { get; private set; }

    public string? FunctionInput { get; private set; }

    public bool FunctionIsDurable { get; private set; }

    public double TimeoutMinutes { get; [UsedImplicitly] private set; }

    public IEnumerable<FunctionStepExecutionParameter> StepExecutionParameters { get; } = new List<FunctionStepExecutionParameter>();

    public override FunctionStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new FunctionStepExecutionAttempt((FunctionStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
        {
            ExecutionStatus = withStatus
        };
        AddAttempt(next);
        return next;
    }

    /// <summary>
    /// Get the <see cref="FunctionApp"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetApp(FunctionApp?)"/> will need to have been called first for the <see cref="FunctionApp"/> to be available.
    /// </summary>
    /// <returns><see cref="FunctionApp"/> if it was previously set using <see cref="SetApp(FunctionApp?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public FunctionApp? GetApp() => _app;

    /// <summary>
    /// Set the private <see cref="FunctionApp"/> object used for containing a possible app reference.
    /// It can be later accessed using <see cref="GetApp"/>.
    /// </summary>
    /// <param name="app"><see cref="FunctionApp"/> reference to store.
    /// The FunctionAppIds are compared and the value is set only if the ids match.</param>
    public void SetApp(FunctionApp? app)
    {
        if (app?.FunctionAppId == FunctionAppId)
        {
            _app = app;
        }
    }

    // Use a field excluded from the EF model to store the app reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    private FunctionApp? _app;
}
