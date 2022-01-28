using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Ui.Shared.JobDetails.StepEdit.StepEditModal;

public partial class PipelineStepEditModal : ParameterizedStepEditModal<PipelineStep>
{
    [Parameter] public IList<DataFactory>? DataFactories { get; set; }

    internal override string FormId => "pipeline_step_edit_form";

    private PipelineSelectOffcanvas PipelineSelectOffcanvas { get; set; } = null!;

    protected override PipelineStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            DataFactoryId = DataFactories?.FirstOrDefault()?.DataFactoryId,
            StepParameters = new List<StepParameterBase>(),
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>()
        };

    protected override Task<PipelineStep> GetExistingStepAsync(EtlManagerContext context, Guid stepId) =>
        context.PipelineSteps
        .Include(step => step.StepParameters)
        .ThenInclude(p => p.JobParameter)
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .FirstAsync(step => step.StepId == stepId);

    private Task OpenPipelineSelectOffcanvas() => PipelineSelectOffcanvas.ShowAsync();

    private void OnPipelineSelected(string pipelineName) => Step.PipelineName = pipelineName;
}
