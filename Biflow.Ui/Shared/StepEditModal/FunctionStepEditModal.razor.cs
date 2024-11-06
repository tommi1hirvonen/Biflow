using Biflow.Ui.Shared.StepEdit;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class FunctionStepEditModal(
    ToasterService toaster, IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<FunctionStep>(toaster, dbContextFactory)
{
    [Parameter] public IList<FunctionApp> FunctionApps { get; set; } = [];

    internal override string FormId => "function_step_edit_form";

    private const string ParametersInfoContent = """
        <div>
            <p>Use parameters to dynamically pass values to the function input (request body).</p>
            <p>
                Parameters are matched based on their names. For example, say you have defined a parameter named <code>@ModifiedSince</code> with a value of <code>2024-09-01</code>.
                A request body like <code>{ "ModifiedSince": "@ModifiedSince" }</code> will become <code>{ "ModifiedSince": "2024-09-01" }</code>
            </p>
        </div>
        """;

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
            FunctionAppId = FunctionApps.First().FunctionAppId
        };

    private enum InputLanguage { Text, Json }
}
