using Biflow.Ui.Shared.StepEdit;
using Biflow.Ui.SqlMetadataExtensions;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class SqlStepEditModal(
    ToasterService toaster, IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<SqlStep>(toaster, dbContextFactory)
{
    internal override string FormId => "sql_step_edit_form";

    private const string ParametersInfoContent = """
        <div>
            <p>Use parameters to dynamically pass values to the SQL statement during execution.</p>
            <p>
                Parameters are matched based on their names. For example, say you have defined a parameter named <code>@MyStringParam</code> with a value of <code>Hello World!</code>.
                A SQL statement like <code>exec MyProcedure @MyStringParam</code> will become <code>exec MyProcedure 'Hello World!'</code>
            </p>
        </div>
        """;

    private StoredProcedureSelectOffcanvas? storedProcedureSelectModal;
    private CodeEditor? editor;
    
    private SqlConnectionBase? Connection
    {
        get
        {
            if (field is null || field.ConnectionId != Step?.ConnectionId)
            {
                field = SqlConnections.FirstOrDefault(c => c.ConnectionId == Step?.ConnectionId) ?? SqlConnections.First();
            }
            return field;
        }
    }

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
            catch { /* ignored */ }
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

    private Task OpenStoredProcedureSelectModal()
    {
        var connection = Connection;
        ArgumentNullException.ThrowIfNull(connection);
        return storedProcedureSelectModal.LetAsync(x => x.ShowAsync(connection));
    }

    private async Task ImportParametersAsync()
    {
        try
        {
            ArgumentNullException.ThrowIfNull(Step);

            if (Connection is MsSqlConnection msSql)
            {
                var procedure = MsSqlExtensions.ParseStoredProcedureFromSqlStatement(Step.SqlStatement);
                if (procedure?.ProcedureName is null || Step.ConnectionId == Guid.Empty)
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
            else
            {
                throw new ArgumentException($"Unsupported connection type: {Connection?.GetType().Name}");
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
