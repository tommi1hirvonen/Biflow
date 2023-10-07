using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Shared.StepEdit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class QlikStepEditModal : StepEditModal<QlikStep>
{
    [Parameter] public IList<QlikCloudClient>? Clients { get; set; }

    internal override string FormId => "qlik_step_edit_form";

    private AppSelectOffcanvas? appSelectOffcanvas;
    private string? appName;

    protected override async Task<QlikStep> GetExistingStepAsync(AppDbContext context, Guid stepId)
    {
        appName = null;
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
        appName = "";
        var client = Clients?.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(client);
        return new(job.JobId, "")
        {
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            TimeoutMinutes = 0,
            QlikCloudClientId = client.QlikCloudClientId,
            QlikCloudClient = client,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            Sources = new List<DataObject>(),
            Targets = new List<DataObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };
    }

    protected override async Task OnModalShownAsync(QlikStep step)
    {
        try
        {
            var client = Clients?.FirstOrDefault(c => c.QlikCloudClientId == step.QlikCloudClientId);
            appName = client is not null
                ? await client.GetAppNameAsync(step.AppId)
                : "";
        }
        catch
        {
            appName = "";
        }
        finally
        {
            StateHasChanged();
        }
    }

    private void OnAppSelected(QlikApp selected)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.AppId = selected.Id;
        appName = selected.Name;
    }

    private Task OpenAppSelectOffcanvas()
    {
        ArgumentNullException.ThrowIfNull(Step);
        ArgumentNullException.ThrowIfNull(Clients);
        var client = Clients.First(c => c.QlikCloudClientId == Step.QlikCloudClientId);
        return appSelectOffcanvas.LetAsync(x => x.ShowAsync(client));
    }
}
