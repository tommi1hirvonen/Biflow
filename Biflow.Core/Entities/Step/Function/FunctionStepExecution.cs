using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.Core.Entities;

public class FunctionStepExecution : StepExecution, IHasTimeout, IHasStepExecutionParameters<FunctionStepExecutionParameter>
{
    public FunctionStepExecution(string stepName, Guid functionAppId, string functionUrl) : base(stepName, StepType.Function)
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
        StepExecutionAttempts = new[] { new FunctionStepExecutionAttempt(this) };
    }

    [Display(Name = "Function app id")]
    public Guid FunctionAppId { get; private set; }

    [Display(Name = "Function url")]
    [MaxLength(1000)]
    public string FunctionUrl { get; private set; }

    [Display(Name = "Function input")]
    public string? FunctionInput { get; private set; }

    [Display(Name = "Is durable")]
    public bool FunctionIsDurable { get; private set; }

    public double TimeoutMinutes { get; private set; }

    public IList<FunctionStepExecutionParameter> StepExecutionParameters { get; set; } = null!;

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
