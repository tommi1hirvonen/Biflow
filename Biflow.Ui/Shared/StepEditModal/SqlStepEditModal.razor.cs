using Biflow.Ui.Shared.StepEdit;
using Biflow.Ui.SqlServer;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class SqlStepEditModal : StepEditModal<SqlStep>
{
    [Inject] private SqlServerHelperService SqlServerHelper { get; set; } = null!;

    internal override string FormId => "sql_step_edit_form";

    private StoredProcedureSelectOffcanvas? storedProcedureSelectModal;
    private CodeEditor? editor;

    protected override Task<SqlStep> GetExistingStepAsync(AppDbContext context, Guid stepId) =>
        context.SqlSteps
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

    protected override async Task OnModalShownAsync(SqlStep step)
    {
        if (editor is not null)
        {
            try
            {
                await editor.SetValueAsync(step.SqlStatement);
            }
            catch { }
        }
    }

    protected override SqlStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            ConnectionId = Connections.First().ConnectionId
        };

    private Task OpenStoredProcedureSelectModal() => storedProcedureSelectModal.LetAsync(x => x.ShowAsync());

    private async Task ImportParametersAsync()
    {
        try
        {
            ArgumentNullException.ThrowIfNull(Step);
            var procedure = Step.SqlStatement.ParseStoredProcedureFromSqlStatement();
            if (procedure is null || procedure.Value.ProcedureName is null || Step.ConnectionId == Guid.Empty)
            {
                Toaster.AddWarning("Stored procedure could not be parsed from SQL statement");
                return;
            }
            var schema = procedure.Value.Schema ?? "dbo";
            var name = procedure.Value.ProcedureName;
            var parameters = await SqlServerHelper.GetStoredProcedureParametersAsync(Step.ConnectionId, schema, name);
            if (!parameters.Any())
            {
                Toaster.AddInformation($"No parameters for [{schema}].[{name}]");
                return;
            }
            Step.StepParameters.Clear();
            foreach (var parameter in parameters)
            {
                Step.StepParameters.Add(new SqlStepParameter
                {
                    ParameterName = parameter.ParameterName,
                    ParameterValue = new Biflow.Core.Entities.ParameterValue
                    {
                        ValueType = parameter.ParameterType
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error importing parameters", ex.Message);
        }
    }

    private Task OnStoredProcedureSelected(string procedure)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(Step);
        Step.SqlStatement = $"EXEC {procedure}";
        return editor.SetValueAsync(Step.SqlStatement);
    }
    
}
