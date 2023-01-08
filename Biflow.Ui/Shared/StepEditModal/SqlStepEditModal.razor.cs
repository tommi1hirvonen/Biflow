using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Biflow.Ui.Shared.StepEdit;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class SqlStepEditModal : ParameterizedStepEditModal<SqlStep>
{
    [Inject] private SqlServerHelperService SqlServerHelper { get; set; } = null!;

    internal override string FormId => "sql_step_edit_form";

    private StoredProcedureSelectOffcanvas? StoredProcedureSelectModal { get; set; }
    
    protected override Task<SqlStep> GetExistingStepAsync(BiflowContext context, Guid stepId) =>
        context.SqlSteps
        .Include(step => step.StepParameters)
        .ThenInclude(p => p.InheritFromJobParameter)
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);

    protected override SqlStep CreateNewStep(Job job) =>
        new()
        {
            JobId = job.JobId,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            ConnectionId = Connections?.FirstOrDefault()?.ConnectionId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            StepParameters = new List<StepParameterBase>(),
            Sources = new List<DataObject>(),
            Targets = new List<DataObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };

    private void SetCaptureResultJobParameter(bool enabled)
    {
        ArgumentNullException.ThrowIfNull(Step);
        if (enabled)
        {
            Step.ResultCaptureJobParameterId = Job?.JobParameters.FirstOrDefault()?.ParameterId;
        }
        else
        {
            Step.ResultCaptureJobParameterId = null;
        }
    }

    private Task OpenStoredProcedureSelectModal() => StoredProcedureSelectModal.LetAsync(x => x.ShowAsync());

    private async Task ImportParametersAsync()
    {
        try
        {
            var procedure = Step?.SqlStatement?.ParseStoredProcedureFromSqlStatement();
            if (procedure is null || procedure.Value.ProcedureName is null || Step?.ConnectionId is null)
            {
                Messenger.AddWarning("Stored procedure could not be parsed from SQL statement");
                return;
            }
            var schema = procedure.Value.Schema ?? "dbo";
            var name = procedure.Value.ProcedureName;
            var parameters = await SqlServerHelper.GetStoredProcedureParameters((Guid)Step.ConnectionId, schema, name);
            if (!parameters.Any())
            {
                Messenger.AddInformation($"No parameters for [{schema}].[{name}]");
                return;
            }
            Step.StepParameters.Clear();
            foreach (var parameter in parameters)
            {
                Step.StepParameters.Add(new StepParameter
                {
                    ParameterName = parameter.ParameterName,
                    ParameterValueType = parameter.ParameterType
                });
            }
        }
        catch (Exception ex)
        {
            Messenger.AddError("Error importing parameters", ex.Message);
        }
    }

    private void OnStoredProcedureSelected(string procedure)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.SqlStatement = $"EXEC {procedure}";
    }
    
}
