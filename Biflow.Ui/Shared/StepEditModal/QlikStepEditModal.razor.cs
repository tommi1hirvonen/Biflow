using Biflow.Ui.Shared.StepEdit;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class QlikStepEditModal : StepEditModal<QlikStep>
{
    [Inject] private IHttpClientFactory HttpClientFactory { get; set; } = null!;

    internal override string FormId => "qlik_step_edit_form";

    private AppSelectOffcanvas? appSelectOffcanvas;
    private QlikApp[]? apps;
    private QlikAutomation[]? automations;

    protected override async Task<QlikStep> GetExistingStepAsync(AppDbContext context, Guid stepId)
    {
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
        var client = QlikClients?.FirstOrDefault();
        ArgumentNullException.ThrowIfNull(client);
        return new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            TimeoutMinutes = 0,
            QlikCloudEnvironmentId = client.QlikCloudEnvironmentId
        };
    }

    protected override Task OnSubmitAsync(AppDbContext context, QlikStep step)
    {
        // Store the app and automation names only for audit purposes.
        if (step.QlikStepSettings is QlikAppReloadSettings reload)
        {
            reload.AppName ??= apps
                ?.FirstOrDefault(a => a.Id == reload.AppId)
                ?.Name;
        }
        else if (step.QlikStepSettings is QlikAutomationRunSettings run)
        {
            run.AutomationName ??= automations
                ?.FirstOrDefault(a => a.Id == run.AutomationId)
                ?.Name;
        }

        // Change tracking does not identify changes to cluster configuration.
        // Tell the change tracker that the config has changed just in case.
        context.Entry(step).Property(p => p.QlikStepSettings).IsModified = true;
        return Task.CompletedTask;
    }

    private void OnAppSelected(QlikApp selected)
    {
        ArgumentNullException.ThrowIfNull(Step);
        if (Step.QlikStepSettings is QlikAppReloadSettings reload)
        {
            reload.AppId = selected.Id;
            reload.AppName = selected.Name;
        }
    }

    private Task OpenAppSelectOffcanvas()
    {
        ArgumentNullException.ThrowIfNull(Step);
        return appSelectOffcanvas.LetAsync(x => x.ShowAsync(Step.QlikCloudEnvironmentId));
    }

    private async Task<QlikApp?> ResolveAppFromValueAsync(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        if (apps is null)
        {
            try
            {
                var workspace = QlikClients?.FirstOrDefault();
                ArgumentNullException.ThrowIfNull(workspace);
                using var client = workspace.CreateClient(HttpClientFactory);
                return await client.GetAppAsync(value);
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching Qlik app", ex.Message);
                return null;
            }
        }
        return apps.FirstOrDefault(a => a.Id == value);
    }

    private async Task<AutosuggestDataProviderResult<QlikApp>> ProvideAppSuggestionsAsync(
        AutosuggestDataProviderRequest request)
    {
        if (apps is null)
        {
            try
            {
                var workspace = QlikClients?.FirstOrDefault();
                ArgumentNullException.ThrowIfNull(workspace);
                using var client = workspace.CreateClient(HttpClientFactory);
                var spaces = await client.GetAppsAsync();
                apps = spaces.SelectMany(s => s.Apps).OrderBy(a => a.Name).ToArray();
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching Qlik apps", ex.Message);
                apps = [];
            }
        }

        return new()
        {
            Data = apps
                .Where(n => n.Name.ContainsIgnoreCase(request.UserInput))
        };
    }

    private async Task<QlikAutomation?> ResolveAutomationFromValueAsync(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        if (automations is null)
        {
            try
            {
                var workspace = QlikClients?.FirstOrDefault();
                ArgumentNullException.ThrowIfNull(workspace);
                using var client = workspace.CreateClient(HttpClientFactory);
                return await client.GetAutomationAsync(value);
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching Qlik automation", ex.Message);
                return null;
            }
        }
        return automations.FirstOrDefault(a => a.Id == value);
    }

    private async Task<AutosuggestDataProviderResult<QlikAutomation>> ProvideAutomationSuggestionsAsync(
        AutosuggestDataProviderRequest request)
    {
        if (automations is null)
        {
            try
            {
                var workspace = QlikClients?.FirstOrDefault();
                ArgumentNullException.ThrowIfNull(workspace);
                using var client = workspace.CreateClient(HttpClientFactory);
                var automations = await client.GetAutomationsAsync();
                this.automations = automations.OrderBy(a => a.Name).ToArray();
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching Qlik automations", ex.Message);
                automations = [];
            }
        }

        return new()
        {
            Data = automations
                .Where(n => n.Name.ContainsIgnoreCase(request.UserInput))
        };
    }
}
