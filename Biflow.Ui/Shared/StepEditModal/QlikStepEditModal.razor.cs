using Biflow.DataAccess;
using Biflow.Ui.Core;
using Biflow.Ui.Shared.StepEdit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class QlikStepEditModal : StepEditModal<QlikStep>
{
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = null!;

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
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstAsync(step => step.StepId == stepId);
        return step;
    }

    protected override QlikStep CreateNewStep(Job job)
    {
        appName = "";
        var client = QlikClients?.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(client);
        return new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            TimeoutMinutes = 0,
            QlikCloudClientId = client.QlikCloudClientId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            DataObjects = new List<StepDataObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };
    }

    protected override async Task OnModalShownAsync(QlikStep step)
    {
        try
        {
            var client = QlikClients?.FirstOrDefault(c => c.QlikCloudClientId == step.QlikCloudClientId);
            ArgumentNullException.ThrowIfNull(client);
            using var connectedClient = client.CreateConnectedClient(HttpClientFactory);
            appName = !string.IsNullOrEmpty(step.AppId)
                ? await connectedClient.GetAppNameAsync(step.AppId)
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
        return appSelectOffcanvas.LetAsync(x => x.ShowAsync(Step.QlikCloudClientId));
    }
}
