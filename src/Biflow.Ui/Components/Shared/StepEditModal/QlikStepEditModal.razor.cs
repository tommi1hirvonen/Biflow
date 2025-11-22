using Biflow.Ui.Components.Shared.StepEdit;

namespace Biflow.Ui.Components.Shared.StepEditModal;

public partial class QlikStepEditModal(
    IHttpClientFactory httpClientFactory,
    IMediator mediator,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<QlikStep>(mediator, toaster, dbContextFactory)
{

    internal override string FormId => "qlik_step_edit_form";

    private AppSelectOffcanvas? _appSelectOffcanvas;
    private QlikApp[]? _apps;
    private QlikAutomation[]? _automations;

    private QlikCloudEnvironment? CurrentEnvironment =>
        Integrations.QlikCloudClients.FirstOrDefault(c => c.QlikCloudEnvironmentId == Step?.QlikCloudEnvironmentId);

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
        var client = Integrations.QlikCloudClients.FirstOrDefault();
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
    
    protected override async Task<QlikStep> OnSubmitCreateAsync(QlikStep step)
    {
        // Store the app and automation names only for audit purposes.
        if (step.QlikStepSettings is QlikAppReloadSettings reload)
        {
            reload.AppName ??= _apps
                ?.FirstOrDefault(a => a.Id == reload.AppId)
                ?.Name;
        }
        else if (step.QlikStepSettings is QlikAutomationRunSettings run)
        {
            run.AutomationName ??= _automations
                ?.FirstOrDefault(a => a.Id == run.AutomationId)
                ?.Name;
        }
        
        var dependencies = step.Dependencies.ToDictionary(
            key => key.DependantOnStepId,
            value => value.DependencyType);
        var executionConditionParameters = step.ExecutionConditionParameters
            .Select(p => new CreateExecutionConditionParameter(
                p.ParameterName,
                p.ParameterValue,
                p.JobParameterId))
            .ToArray();
        var command = new CreateQlikStepCommand
        {
            JobId = step.JobId,
            StepName = step.StepName ?? "",
            StepDescription = step.StepDescription,
            ExecutionPhase = step.ExecutionPhase,
            DuplicateExecutionBehaviour = step.DuplicateExecutionBehaviour,
            IsEnabled = step.IsEnabled,
            RetryAttempts = step.RetryAttempts,
            RetryIntervalMinutes = step.RetryIntervalMinutes,
            ExecutionConditionExpression = step.ExecutionConditionExpression.Expression,
            StepTagIds = step.Tags.Select(t => t.TagId).ToArray(),
            TimeoutMinutes = step.TimeoutMinutes,
            QlikCloudEnvironmentId = step.QlikCloudEnvironmentId,
            Settings = step.QlikStepSettings,
            Dependencies = dependencies,
            ExecutionConditionParameters = executionConditionParameters,
            Sources = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Source)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Targets = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Target)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray()
        };
        return await Mediator.SendAsync(command);
    }

    protected override async Task<QlikStep> OnSubmitUpdateAsync(QlikStep step)
    {
        // Store the app and automation names only for audit purposes.
        if (step.QlikStepSettings is QlikAppReloadSettings reload)
        {
            reload.AppName ??= _apps
                ?.FirstOrDefault(a => a.Id == reload.AppId)
                ?.Name;
        }
        else if (step.QlikStepSettings is QlikAutomationRunSettings run)
        {
            run.AutomationName ??= _automations
                ?.FirstOrDefault(a => a.Id == run.AutomationId)
                ?.Name;
        }
        
        var dependencies = step.Dependencies.ToDictionary(
            key => key.DependantOnStepId,
            value => value.DependencyType);
        var executionConditionParameters = step.ExecutionConditionParameters
            .Select(p => new UpdateExecutionConditionParameter(
                p.ParameterId,
                p.ParameterName,
                p.ParameterValue,
                p.JobParameterId))
            .ToArray();
        var command = new UpdateQlikStepCommand
        {
            StepId = step.StepId,
            StepName = step.StepName ?? "",
            StepDescription = step.StepDescription,
            ExecutionPhase = step.ExecutionPhase,
            DuplicateExecutionBehaviour = step.DuplicateExecutionBehaviour,
            IsEnabled = step.IsEnabled,
            RetryAttempts = step.RetryAttempts,
            RetryIntervalMinutes = step.RetryIntervalMinutes,
            ExecutionConditionExpression = step.ExecutionConditionExpression.Expression,
            StepTagIds = step.Tags.Select(t => t.TagId).ToArray(),
            TimeoutMinutes = step.TimeoutMinutes,
            QlikCloudEnvironmentId = step.QlikCloudEnvironmentId,
            Settings = step.QlikStepSettings,
            Dependencies = dependencies,
            ExecutionConditionParameters = executionConditionParameters,
            Sources = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Source)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Targets = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Target)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray()
        };
        return await Mediator.SendAsync(command);
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
        return _appSelectOffcanvas.LetAsync(x => x.ShowAsync(Step.QlikCloudEnvironmentId));
    }

    private async Task<QlikApp?> ResolveAppFromValueAsync(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        if (_apps is null)
        {
            try
            {
                var environment = CurrentEnvironment;
                ArgumentNullException.ThrowIfNull(environment);
                var client = environment.CreateClient(httpClientFactory);
                return await client.GetAppAsync(value);
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching Qlik app", ex.Message);
                return null;
            }
        }
        return _apps.FirstOrDefault(a => a.Id == value);
    }

    private async Task<AutosuggestDataProviderResult<QlikApp>> ProvideAppSuggestionsAsync(
        AutosuggestDataProviderRequest request)
    {
        if (_apps is null)
        {
            try
            {
                var environment = CurrentEnvironment;
                ArgumentNullException.ThrowIfNull(environment);
                var client = environment.CreateClient(httpClientFactory);
                var spaces = await client.GetAppsAsync();
                _apps = spaces.SelectMany(s => s.Apps).OrderBy(a => a.Name).ToArray();
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching Qlik apps", ex.Message);
                _apps = [];
            }
        }

        return new()
        {
            Data = _apps
                .Where(n => n.Name.ContainsIgnoreCase(request.UserInput))
        };
    }

    private async Task<QlikAutomation?> ResolveAutomationFromValueAsync(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        if (_automations is null)
        {
            try
            {
                var environment = CurrentEnvironment;
                ArgumentNullException.ThrowIfNull(environment);
                var client = environment.CreateClient(httpClientFactory);
                return await client.GetAutomationAsync(value);
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching Qlik automation", ex.Message);
                return null;
            }
        }
        return _automations.FirstOrDefault(a => a.Id == value);
    }

    private async Task<AutosuggestDataProviderResult<QlikAutomation>> ProvideAutomationSuggestionsAsync(
        AutosuggestDataProviderRequest request)
    {
        if (_automations is null)
        {
            try
            {
                var environment = CurrentEnvironment;
                ArgumentNullException.ThrowIfNull(environment);
                var client = environment.CreateClient(httpClientFactory);
                var automations = await client.GetAutomationsAsync();
                _automations = automations.OrderBy(a => a.Name).ToArray();
            }
            catch (Exception ex)
            {
                Toaster.AddError("Error fetching Qlik automations", ex.Message);
                _automations = [];
            }
        }

        return new()
        {
            Data = _automations
                .Where(n => n.Name.ContainsIgnoreCase(request.UserInput))
        };
    }
}
