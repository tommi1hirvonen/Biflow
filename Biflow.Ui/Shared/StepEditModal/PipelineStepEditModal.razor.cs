using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Shared.StepEdit;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class PipelineStepEditModal : ParameterizedStepEditModal<PipelineStep>
{
    [Parameter] public IList<PipelineClient>? PipelineClients { get; set; }

    [Inject] private ITokenService TokenService { get; set; } = null!;

    internal override string FormId => "pipeline_step_edit_form";

    private PipelineSelectOffcanvas PipelineSelectOffcanvas { get; set; } = null!;

    protected override PipelineStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            PipelineClientId = PipelineClients?.FirstOrDefault()?.PipelineClientId,
            StepParameters = new List<StepParameterBase>(),
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            Sources = new List<SourceTargetObject>(),
            Targets = new List<SourceTargetObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };

    protected override Task<PipelineStep> GetExistingStepAsync(BiflowContext context, Guid stepId) =>
        context.PipelineSteps
        .Include(step => step.StepParameters)
        .ThenInclude(p => p.JobParameter)
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);

    private async Task ImportParametersAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Step?.PipelineName))
            {
                Messenger.AddWarning("Pipeline name was empty");
                return;
            }
            using var context = await DbContextFactory.CreateDbContextAsync();
            var client = await context.PipelineClients
                .AsNoTrackingWithIdentityResolution()
                .Include(c => c.AppRegistration)
                .FirstAsync(c => c.PipelineClientId == Step.PipelineClientId);
            var parameters = await client.GetPipelineParametersAsync(TokenService, Step.PipelineName);
            if (!parameters.Any())
            {
                Messenger.AddInformation($"No parameters for pipeline {Step.PipelineName}");
                return;
            }
            Step.StepParameters.Clear();
            foreach (var param in parameters)
            {
                Step.StepParameters.Add(new StepParameter
                {
                    ParameterName = param.Name,
                    ParameterValueType = param.Type,
                    ParameterValue = param.Default
                });
            }
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error importing parameters", ex.Message);
        }
    }

    private Task OpenPipelineSelectOffcanvas() => PipelineSelectOffcanvas.ShowAsync();

    private void OnPipelineSelected(string pipelineName)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.PipelineName = pipelineName;
    }
}
