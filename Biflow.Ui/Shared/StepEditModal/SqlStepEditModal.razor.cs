using Biflow.Ui.Shared.StepEdit;
using Biflow.Ui.SqlMetadataExtensions;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class SqlStepEditModal : StepEditModal<SqlStep>
{
    internal override string FormId => "sql_step_edit_form";

    private StoredProcedureSelectOffcanvas? storedProcedureSelectModal;
    private CodeEditor? editor;
    
    private ConnectionBase Connection
    {
        get
        {
            if (_connection is null || _connection.ConnectionId != Step?.ConnectionId)
            {
                _connection = SqlConnections.FirstOrDefault(c => c.ConnectionId == Step?.ConnectionId) ?? SqlConnections.First();
            }
            return _connection;
        }
    }

    private ConnectionBase? _connection = null;

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
            ConnectionId = SqlConnections.First().ConnectionId
        };

    private Task OpenStoredProcedureSelectModal() => storedProcedureSelectModal.LetAsync(x => x.ShowAsync(Connection));

    private async Task ImportParametersAsync()
    {
        try
        {
            ArgumentNullException.ThrowIfNull(Step);

            if (Connection is MsSqlConnection msSql)
            {
                var procedure = Step.SqlStatement.ParseStoredProcedureFromSqlStatement();
                if (procedure is null || procedure.Value.ProcedureName is null || Step.ConnectionId == Guid.Empty)
                {
                    Toaster.AddWarning("Stored procedure could not be parsed from SQL statement");
                    return;
                }
                var procSchema = procedure.Value.Schema ?? "dbo";
                var procName = procedure.Value.ProcedureName;
                var parameters = await msSql.GetStoredProcedureParametersAsync(procSchema, procName);
                if (!parameters.Any())
                {
                    Toaster.AddInformation($"No parameters for [{procSchema}].[{procName}]");
                    return;
                }

                Step.StepParameters.Clear();
                foreach (var (paramName, paramValue) in parameters)
                {
                    Step.StepParameters.Add(new SqlStepParameter
                    {
                        ParameterName = paramName,
                        ParameterValue = paramValue
                    });
                }
            }
            else if (Connection is SnowflakeConnection snow)
            {
                // TODO Handle Snowflake
            }
            else
            {
                throw new ArgumentException($"Unsupported connection type: {Connection.GetType().Name}");
            }
        }
        catch (Exception ex)
        {
            Toaster.AddError("Error importing parameters", ex.Message);
        }
    }

    private Task OnStoredProcedureSelected(IStoredProcedure procedure)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(Step);
        Step.SqlStatement = procedure.InvokeSqlStatement;
        return editor.SetValueAsync(Step.SqlStatement);
    }
    
}
