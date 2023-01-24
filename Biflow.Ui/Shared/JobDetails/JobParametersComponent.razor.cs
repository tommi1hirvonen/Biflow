using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;

namespace Biflow.Ui.Shared.JobDetails;

public partial class JobParametersComponent : ComponentBase, IDisposable
{
    [Inject] private IDbContextFactory<BiflowContext> DbContextFactory { get; set; } = null!;
    
    [Inject] private IHxMessengerService Messenger { get; set; } = null!;

    [Inject] private IJSRuntime JS { get; set; } = null!;

    [CascadingParameter] public Job? Job { get; set; }

    [CascadingParameter] public List<Step>? Steps { get; set; }

    private Job? EditJob { get; set; }

    private BiflowContext? Context { get; set; }

    private string? ErrorMessage { get; set; }

    private bool Loading { get; set; } = false;

    private HxOffcanvas? ExpressionEditOffcanvas { get; set; }

    private JobParameter? ExpressionEditParameter { get; set; }

    private HxOffcanvas? ReferencingStepsOffcanvas { get; set; }
    
    private ReferencingStepsModel ReferencingSteps { get; set; } = new(new(), Enumerable.Empty<Step>(), Enumerable.Empty<Step>(), Enumerable.Empty<Step>());

    protected override async Task OnParametersSetAsync()
    {
        if (Job is null || Loading || Job.JobId == EditJob?.JobId)
        {
            return;
        }
        Loading = true;
        Context?.Dispose();
        Context = DbContextFactory.CreateDbContext();
        EditJob = await Context.Jobs
            .Include(j => j.JobParameters)
            .ThenInclude(j => j.AssigningStepParameters)
            .ThenInclude(p => p.Step) // Assigning steps are from other jobs, which means they are not in the Steps List property
            .ThenInclude(s => s.Job)
            .FirstAsync(j => j.JobId == Job.JobId);
        Loading = false;
    }

    private void AddParameter() => EditJob?.JobParameters
        .Insert(0, new JobParameter { ParameterValueType = ParameterValueType.String, AssigningStepParameters = new List<JobStepParameter>() });

    private async Task SubmitParameters()
    {
        ErrorMessage = null;
        var (paramResult, paramMessage) = ParametersCheck();
        if (!paramResult)
        {
            ErrorMessage = paramMessage;
            return;
        }

        foreach (var param in EditJob?.JobParameters ?? Enumerable.Empty<JobParameter>())
        {
            // Update the referencing job step parameter names to match the possibly changed new name.
            foreach (var referencingJobStepParam in param.AssigningStepParameters)
            {
                referencingJobStepParam.ParameterName = param.ParameterName;
            }
        }

        try
        {
            ArgumentNullException.ThrowIfNull(Context);
            await Context.SaveChangesAsync();
            Messenger.AddInformation("Job parameters updated successfully");
        }
        catch (DbUpdateConcurrencyException)
        {
            Messenger.AddError("Concurrency error",
                "The job has been modified outside of this session. Reload the page to view the most recent settings.");
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error saving parameters", $"{ex.Message}\n{ex.InnerException?.Message}");
        }

    }

    private (bool Result, string? Message) ParametersCheck()
    {
        var parameters = EditJob?.JobParameters
            .OrderBy(param => param.ParameterName)
            .ToList() ?? Enumerable.Empty<JobParameter>().ToList();
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

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        if (!Context?.ChangeTracker.HasChanges() ?? true)
        {
            return;
        }

        var confirmed = await JS.InvokeAsync<bool>("confirm", "Discard unsaved changes?");
        if (!confirmed)
        {
            context.PreventNavigation();
        }
    }

    private async Task ShowExpressionEditOffcanvasAsync(DynamicParameter parameter)
    {
        ExpressionEditParameter = (JobParameter)parameter;
        await ExpressionEditOffcanvas.LetAsync(x => x.ShowAsync());
    }

    private async Task TestEvaluate(JobParameter parameter)
    {
        try
        {
            var result = await parameter.Expression.EvaluateAsync();
            Messenger.AddInformation($"Evaluation result: {result}");
        }
        catch (Exception ex)
        {
            Messenger.AddError(ex.Message);
        }
    }

    private async Task ShowReferencingStepsAsync(JobParameter param)
    {
        ReferencingSteps = new(param, GetInheritingSteps(param), GetCapturingSteps(param), GetAssigningSteps(param));
        await ReferencingStepsOffcanvas.LetAsync(x => x.ShowAsync());
    }

    private IEnumerable<Step> GetInheritingSteps(JobParameter parameter) => Steps
        ?.Where(s => s is IHasStepParameters hasParams && hasParams.StepParameters.Any(p => p.InheritFromJobParameterId == parameter.ParameterId))
        .OrderBy(s => s.StepName)
        ?? Enumerable.Empty<Step>();

    private IEnumerable<Step> GetCapturingSteps(JobParameter parameter) => Steps
        ?.Where(s => s is SqlStep sql && sql.ResultCaptureJobParameterId == parameter.ParameterId)
        .OrderBy(s => s.StepName)
        ?? Enumerable.Empty<Step>();

    private static IEnumerable<Step> GetAssigningSteps(JobParameter parameter) => parameter.AssigningStepParameters
        .Select(p => p.Step)
        .OrderBy(s => s.Job.JobName)
        .ThenBy(s => s.StepName)
        ?? Enumerable.Empty<Step>();

    public void Dispose() => Context?.Dispose();

    private record ReferencingStepsModel(
        JobParameter Parameter,
        IEnumerable<Step> InheritingSteps,
        IEnumerable<Step> CapturingSteps,
        IEnumerable<Step> AssigningSteps);

}
