namespace Biflow.Ui.Shared.StepEditModal;

public partial class EmailStepEditModal : StepEditModal<EmailStep>
{
    internal override string FormId => "email_step_edit_form";

    private const string ParametersInfoContent = """
        <div>
            <p>Use parameters to dynamically pass values to the recipients, subject and body properties.</p>
            <p>
                Parameters are matched based on their names. For example, say you have defined a parameter named <code>@Recipient_123</code> with a value of <code>recipient@mycompany.com</code>.
                A recipients list like <code>@Recipient_123, other_recipient@mycompany.com</code> will become <code>recipient@mycompany.com, other_recipient@mycompany.com</code>
            </p>
        </div>
        """;

    protected override Task<EmailStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.EmailSteps
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

    protected override EmailStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0
        };
}
