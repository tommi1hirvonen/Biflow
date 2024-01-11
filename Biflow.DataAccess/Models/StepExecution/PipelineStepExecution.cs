using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class PipelineStepExecution : StepExecution, IHasTimeout, IHasStepExecutionParameters<PipelineStepExecutionParameter>
{
    public PipelineStepExecution(string stepName, string pipelineName) : base(stepName, StepType.Pipeline)
    {
        PipelineName = pipelineName;
    }

    public PipelineStepExecution(PipelineStep step, Execution execution) : base(step, execution)
    {
        ArgumentNullException.ThrowIfNull(step.PipelineName);
        ArgumentNullException.ThrowIfNull(step.PipelineClientId);

        PipelineName = step.PipelineName;
        PipelineClientId = step.PipelineClientId;
        TimeoutMinutes = step.TimeoutMinutes;
        StepExecutionParameters = step.StepParameters
            .Select(p => new PipelineStepExecutionParameter(p, this))
            .ToArray();
        StepExecutionAttempts = new[] { new PipelineStepExecutionAttempt(this) };
    }

    [Display(Name = "Pipeline name")]
    [MaxLength(250)]
    public string PipelineName { get; private set; }

    [Display(Name = "Pipeline client id")]
    public Guid PipelineClientId { get; private set; }

    [Column("TimeoutMinutes")]
    public double TimeoutMinutes { get; private set; }

    public IList<PipelineStepExecutionParameter> StepExecutionParameters { get; set; } = null!;

    /// <summary>
    /// Get the <see cref="PipelineClient"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetClient(PipelineClient?)"/> will need to have been called first for the <see cref="PipelineClient"/> to be available.
    /// </summary>
    /// <returns><see cref="PipelineClient"/> if it was previously set using <see cref="SetClient(PipelineClient?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public PipelineClient? GetClient() => _client;

    /// <summary>
    /// Set the private <see cref="PipelineClient"/> object used for containing a possible client reference.
    /// It can be later accessed using <see cref="GetClient"/>.
    /// </summary>
    /// <param name="client"><see cref="PipelineClient"/> reference to store.
    /// The PipelineClientIds are compared and the value is set only if the ids match.</param>
    public void SetClient(PipelineClient? client)
    {
        if (client?.PipelineClientId == PipelineClientId)
        {
            _client = client;
        }
    }

    // Use a field excluded from the EF model to store the client reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    [NotMapped]
    private PipelineClient? _client;
}
