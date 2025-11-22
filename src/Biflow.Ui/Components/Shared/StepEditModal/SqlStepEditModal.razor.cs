using System.Diagnostics.CodeAnalysis;
using Biflow.Ui.Components.Shared.StepEdit;
using Biflow.Ui.SqlMetadataExtensions;

namespace Biflow.Ui.Components.Shared.StepEditModal;

public partial class SqlStepEditModal(
    IMediator mediator,
    ToasterService toaster,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : StepEditModal<SqlStep>(mediator, toaster, dbContextFactory)
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

    private StoredProcedureSelectOffcanvas? _storedProcedureSelectModal;
    private CodeEditor? _editor;

    [field: AllowNull, MaybeNull]
    private SqlConnectionBase Connection
    {
        get
        {
            if (field is null || field.ConnectionId != Step?.ConnectionId)
            {
                field = Integrations.SqlConnections.FirstOrDefault(c => c.ConnectionId == Step?.ConnectionId)
                        ?? Integrations.SqlConnections.First();
            }
            return field;
        }
    } = null;

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
    
    protected override SqlStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            Job = job,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            ConnectionId = Integrations.SqlConnections.First().ConnectionId
        };
    
    protected override async Task<SqlStep> OnSubmitCreateAsync(SqlStep step)
    {
        var dependencies = step.Dependencies.ToDictionary(
            key => key.DependantOnStepId,
            value => value.DependencyType);
        var executionConditionParameters = step.ExecutionConditionParameters
            .Select(p => new CreateExecutionConditionParameter(
                p.ParameterName,
                p.ParameterValue,
                p.JobParameterId))
            .ToArray();
        var parameters = step.StepParameters
            .Select(p => new CreateStepParameter(
                p.ParameterName,
                p.ParameterValue,
                p.UseExpression,
                p.Expression.Expression,
                p.InheritFromJobParameterId,
                p.ExpressionParameters
                    .Select(e => new CreateExpressionParameter(e.ParameterName, e.InheritFromJobParameterId))
                    .ToArray()))
            .ToArray();
        var command = new CreateSqlStepCommand
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
            ConnectionId = step.ConnectionId,
            SqlStatement = step.SqlStatement,
            ResultCaptureJobParameterId = step.ResultCaptureJobParameterId,
            Dependencies = dependencies,
            ExecutionConditionParameters = executionConditionParameters,
            Sources = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Source)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Targets = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Target)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Parameters = parameters
        };
        return await Mediator.SendAsync(command);
    }

    protected override async Task<SqlStep> OnSubmitUpdateAsync(SqlStep step)
    {
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
        var parameters = step.StepParameters
            .Select(p => new UpdateStepParameter(
                p.ParameterId,
                p.ParameterName,
                p.ParameterValue,
                p.UseExpression,
                p.Expression.Expression,
                p.InheritFromJobParameterId,
                p.ExpressionParameters
                    .Select(e => new UpdateExpressionParameter(
                        e.ParameterId,
                        e.ParameterName,
                        e.InheritFromJobParameterId))
                    .ToArray()))
            .ToArray();
        var command = new UpdateSqlStepCommand
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
            ConnectionId = step.ConnectionId,
            SqlStatement = step.SqlStatement,
            ResultCaptureJobParameterId = step.ResultCaptureJobParameterId,
            Dependencies = dependencies,
            ExecutionConditionParameters = executionConditionParameters,
            Sources = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Source)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Targets = step.DataObjects
                .Where(x => x.ReferenceType == DataObjectReferenceType.Target)
                .Select(x => new DataObjectRelation(x.DataObject.ObjectId, x.DataAttributes.ToArray()))
                .ToArray(),
            Parameters = parameters
        };
        return await Mediator.SendAsync(command);
    }

    protected override async Task OnModalShownAsync(SqlStep step)
    {
        if (_editor is not null)
        {
            try
            {
                await _editor.SetValueAsync(step.SqlStatement);
            }
            catch { /* ignored */ }
        }
    }

    

    private Task OpenStoredProcedureSelectModal()
    {
        var connection = Connection;
        ArgumentNullException.ThrowIfNull(connection);
        return _storedProcedureSelectModal.LetAsync(x => x.ShowAsync(connection));
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
        ArgumentNullException.ThrowIfNull(_editor);
        ArgumentNullException.ThrowIfNull(Step);
        Step.SqlStatement = procedure.InvokeSqlStatement;
        return _editor.SetValueAsync(Step.SqlStatement);
    }
    
}
