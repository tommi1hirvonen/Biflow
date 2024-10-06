namespace Biflow.Ui.Shared.StepEditModal;

public partial class ExeStepEditModal : StepEditModal<ExeStep>
{
    internal override string FormId => "exe_step_edit_form";

    private const string ParametersInfoContent = """
        <div>
            <p>Use parameters to dynamically pass values to the executable arguments property.</p>
            <p>
                Parameters are matched based on their names. For example, say you have defined a parameter named <code>@param1</code> with a value of <code>https://example.com</code>.
                An arguments property like <code>--url @param1</code> will become <code>--url https://example.com</code>
            </p>
        </div>
        """;

    protected override ExeStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0
        };

    protected override Task<ExeStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.ExeSteps
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
}
