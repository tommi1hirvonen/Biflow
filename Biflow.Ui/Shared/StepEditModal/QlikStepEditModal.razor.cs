using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class QlikStepEditModal : StepEditModal<QlikStep>
{
    [Parameter] public IList<QlikCloudClient>? Clients { get; set; }

    internal override string FormId => "qlik_step_edit_form";

    protected override async Task<QlikStep> GetExistingStepAsync(AppDbContext context, Guid stepId)
    {
        var step = await context.QlikSteps
            .Include(step => step.Job)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.Sources)
            .Include(step => step.Targets)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);
        return step;
    }

    protected override QlikStep CreateNewStep(Job job)
    {
        var client = Clients?.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(client);
        return new(job.JobId, "")
        {
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            TimeoutMinutes = 0,
            QlikCloudClientId = client.QlikCloudClientId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            Sources = new List<DataObject>(),
            Targets = new List<DataObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };
    }
}
