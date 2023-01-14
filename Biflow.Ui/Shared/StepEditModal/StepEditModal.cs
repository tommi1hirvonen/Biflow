using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public abstract partial class StepEditModal<TStep> : ComponentBase, IDisposable, IStepEditModal where TStep : Step
{    
    [Inject] public IHxMessengerService Messenger { get; set; } = null!;

    [Inject] public IDbContextFactory<BiflowContext> DbContextFactory { get; set; } = null!;

    [CascadingParameter] public Job? Job { get; set; }

    [CascadingParameter] public List<Step>? Steps { get; set; }

    [Parameter] public Action? OnModalClosed { get; set; }

    [Parameter] public EventCallback<Step> OnStepSubmit { get; set; }
    
    [Parameter] public IEnumerable<SqlConnectionInfo> Connections { get; set; } = Enumerable.Empty<SqlConnectionInfo>();

    internal abstract string FormId { get; }

    internal TStep? Step { get; private set; }

    internal List<string> Tags { get; set; } = new();

    internal HxModal? Modal { get; set; }

    internal string StepError { get; private set; } = string.Empty;

    internal StepEditModalView CurrentView { get; set; } = StepEditModalView.Settings;

    private BiflowContext Context { get; set; } = null!;

    private IEnumerable<Tag>? AllTags { get; set; }

    private IEnumerable<DataObject>? DataObjects { get; set; }

    internal async Task<InputTagsDataProviderResult> GetTagSuggestions(InputTagsDataProviderRequest request)
    {
        AllTags ??= await Context.Tags.ToListAsync();
        return new InputTagsDataProviderResult
        {
            Data = AllTags?
            .Select(t => t.TagName)
            .Where(t => t.ContainsIgnoreCase(request.UserInput))
            .Where(t => !Tags.Any(tag => t == tag))
            .OrderBy(t => t) ?? Enumerable.Empty<string>()
        };
    }

    public async Task<IEnumerable<DataObject>> GetDataObjectsAsync()
    {
        DataObjects ??= await Context.DataObjects.ToListAsync();
        return DataObjects;
    }

    protected virtual (bool Result, string? ErrorMessage) StepValidityCheck(Step step)
    {
        ArgumentNullException.ThrowIfNull(Step);
        (var conditionParamResult, var conditionParamMessage) = ExecutionConditionParametersCheck();
        if (!conditionParamResult)
        {
            return (false, conditionParamMessage);
        }

        foreach (var param in Step.ExecutionConditionParameters)
        {
            param.SetParameterValue();
        }

        (var paramResult, var paramMessage) = ParametersCheck();
        if (!paramResult)
        {
            return (false, paramMessage);
        }

        foreach (var param in Step.StepParameters)
        {
            param.SetParameterValue();
        }
        return (true, null);
    }

    protected virtual (bool Result, string? Message) ParametersCheck()
    {
        ArgumentNullException.ThrowIfNull(Step);
        var parameters = Step.StepParameters.OrderBy(param => param.ParameterName).ToList();
        foreach (var param in parameters)
        {
            if (string.IsNullOrEmpty(param.ParameterName))
            {
                return (false, "Parameter name cannot be empty");
            }
        }
        for (var i = 0; i < parameters.Count - 1; i++)
        {
            if (parameters[i + 1].ParameterName == parameters[i].ParameterName)
            {
                return (false, "Duplicate parameter names");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// Called during OnParametersSetAsync() to load an existing Step from BiflowContext.
    /// The Step loaded from the context should be tracked in order to track changes made to the object.
    /// </summary>
    /// <param name="context">Instance of BiflowContext</param>
    /// <param name="stepId">Id of an existing Step that is to be edited</param>
    /// <returns></returns>
    protected abstract Task<TStep> GetExistingStepAsync(BiflowContext context, Guid stepId);

    /// <summary>
    /// Called during OnParametersSetAsync() if a new Step is being created.
    /// The method should return an "empty" instance of Step with its navigation properties correctly initialized.
    /// </summary>
    /// <param name="job">The job in which the Step is created</param>
    /// <returns></returns>
    protected abstract TStep CreateNewStep(Job job);

    private async Task ResetContext()
    {
        if (Context is not null)
            await Context.DisposeAsync();

        Context = await DbContextFactory.CreateDbContextAsync();
    }

    private void ResetTags() => Tags = Step?.Tags
        .Select(t => t.TagName)
        .OrderBy(t => t)
        .ToList() ?? new();

    public void ResetStepError() => StepError = string.Empty;

    internal void OnClosed()
    {
        Step = null;
        AllTags = null;
        DataObjects = null;
    }

    internal async Task SubmitStep()
    {
        if (Step is null)
        {
            Messenger.AddError("Error submitting step", "Step was null");
            return;
        }

        StepError = string.Empty;

        var (result, message) = StepValidityCheck(Step);
        if (!result)
        {
            StepError = message ?? string.Empty;
            return;
        }

        // Check data objects.
        var (result2, message2) = await CheckDataObjectsAsync();
        if (!result2)
        {
            StepError = message2 ?? string.Empty;
            return;
        }

        // Synchronize tags
        foreach (var text in Tags.Where(str => !Step.Tags.Any(t => t.TagName == str)))
        {
            // New tags
            var tag = AllTags?.FirstOrDefault(t => t.TagName == text) ?? new Tag(text);
            Step.Tags.Add(tag);
        }
        foreach (var tag in Step.Tags.Where(t => !Tags.Contains(t.TagName)).ToList() ?? Enumerable.Empty<Tag>())
        {
            Step.Tags.Remove(tag);
        }

        // Save changes.
        try
        {
            // New step
            if (Step.StepId == Guid.Empty)
            {
                Context.Steps.Add(Step);
            }
            // If the Step was an existing Step, the context has been tracking its changes.
            // => No need to attach it to the context separately.
            await Context.SaveChangesAsync();

            await OnStepSubmit.InvokeAsync(Step);
            await Modal.LetAsync(x => x.HideAsync());
        }
        catch (DbUpdateConcurrencyException)
        {
            Messenger.AddError("Concurrency error",
                "The step has been modified outside of this session. Reload the page to view the most recent settings.");
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error adding/editing step", $"{ex.Message}\n{ex.InnerException?.Message}");
        }
    }

    private async Task<(bool Result, string? Message)> CheckDataObjectsAsync()
    {
        if (Step is null)
        {
            return (false, "Step was null");
        }

        var sources = Step.Sources
            .OrderBy(x => x.ServerName)
            .ThenBy(x => x.DatabaseName)
            .ThenBy(x => x.SchemaName)
            .ThenBy(x => x.ObjectName)
            .ToList();
        if (!CheckDataObjectDuplicates(sources)) return (false, "Duplicate sources");
        
        if (sources.Any(x => !x.ServerName.Any()
        || !x.DatabaseName.Any()
        || !x.SchemaName.Any()
        || !x.ObjectName.Any()))
            return (false, "Empty source object names are not valid");
        
        var targets = Step.Targets
            .OrderBy(x => x.ServerName)
            .ThenBy(x => x.DatabaseName)
            .ThenBy(x => x.SchemaName)
            .ThenBy(x => x.ObjectName)
            .ToList();
        if (!CheckDataObjectDuplicates(targets)) return (false, "Duplicate targets");

        if (targets.Any(x => !x.ServerName.Any()
        || !x.DatabaseName.Any()
        || !x.SchemaName.Any()
        || !x.ObjectName.Any()))
            return (false, "Empty target object names are not valid");

        await MapExistingObjectsAsync(Step.Sources);
        await MapExistingObjectsAsync(Step.Targets);

        return (true, null);
    }

    private static bool CheckDataObjectDuplicates(IEnumerable<DataObject> objects)
    {
        for (int i = 0; i < objects.Count() - 1; i++)
        {
            var current = objects.ElementAt(i);
            var next = objects.ElementAt(i + 1);
            if (current.Equals(next))
            {
                return false;
            }
        }
        return true;
    }

    private async Task MapExistingObjectsAsync(IList<DataObject> objects)
    {
        var allObjects = await GetDataObjectsAsync();
        var existing = allObjects.Where(o => objects.Any(d => d.ObjectId == Guid.Empty && o.Equals(d)));
        foreach (var dbObject in existing)
        {
            var remove = objects.First(d => d.Equals(dbObject));
            objects.Remove(remove);
            objects.Add(dbObject);
        }
    }

    private (bool Result, string? Message) ExecutionConditionParametersCheck()
    {
        ArgumentNullException.ThrowIfNull(Step);
        var parameters = Step.ExecutionConditionParameters.OrderBy(param => param.ParameterName).ToList();
        foreach (var param in parameters)
        {
            if (string.IsNullOrEmpty(param.ParameterName))
            {
                return (false, "Execution condition parameter name cannot be empty");
            }
        }
        for (var i = 0; i < parameters.Count - 1; i++)
        {
            if (parameters[i + 1].ParameterName == parameters[i].ParameterName)
            {
                return (false, "Duplicate execution condition parameter names");
            }
        }

        return (true, null);
    }

    public async Task ShowAsync(Guid stepId, StepEditModalView startView = StepEditModalView.Settings)
    {
        CurrentView = startView;
        await Modal.LetAsync(x => x.ShowAsync());
        await ResetContext();
        if (stepId != Guid.Empty)
        {
            Step = await GetExistingStepAsync(Context, stepId);
        }
        else if (stepId == Guid.Empty && Job is not null)
        {
            Step = CreateNewStep(Job);
        }
        ResetTags();
        StateHasChanged();
    }

    public void Dispose() => Context?.Dispose();
}
