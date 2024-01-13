using Biflow.Ui.Shared.StepEdit;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class FunctionStepEditModal : StepEditModal<FunctionStep>
{
    [Parameter] public IList<FunctionApp> FunctionApps { get; set; } = [];

    internal override string FormId => "function_step_edit_form";

    private FunctionSelectOffcanvas? functionSelectOffcanvas;
    private CodeEditor? editor;
    private InputLanguage language = InputLanguage.Text;

    private Task OpenFunctionSelectOffcanvas()
    {
        ArgumentNullException.ThrowIfNull(Step?.FunctionAppId);
        return functionSelectOffcanvas.LetAsync(x => x.ShowAsync(Step.FunctionAppId));
    }

    private void OnFunctionSelected(string functionUrl)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.FunctionUrl = functionUrl;
    }

    protected override async Task OnModalShownAsync(FunctionStep step)
    {
        language = InputLanguage.Text;
        if (editor is not null)
        {
            try
            {
                await editor.SetValueAsync(step.FunctionInput);
            }
            catch { }
        }
    }

    private Task SetLanguageAsync(InputLanguage language)
    {
        this.language = language;
        ArgumentNullException.ThrowIfNull(editor);
        var lang = this.language switch
        {
            InputLanguage.Json => "json",
            _ => ""
        };
        return editor.SetLanguageAsync(lang);
    }

    protected override Task<FunctionStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.FunctionSteps
        .Include(step => step.Job)
        .ThenInclude(job => job.JobParameters)
        .Include(step => step.StepParameters)
        .ThenInclude(p => p.InheritFromJobParameter)
        .Include(step => step.StepParameters)
        .ThenInclude(p => p.ExpressionParameters)
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.DataObjects)
        .ThenInclude(s => s.DataObject)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);

    protected override FunctionStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            FunctionAppId = FunctionApps.First().FunctionAppId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            StepParameters = new List<FunctionStepParameter>(),
            DataObjects = new List<StepDataObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };

    private enum InputLanguage { Text, Json }
}
