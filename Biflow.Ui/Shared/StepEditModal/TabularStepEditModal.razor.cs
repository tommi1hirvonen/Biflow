using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Shared.StepEdit;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Shared.StepEditModal;

public partial class TabularStepEditModal : StepEditModalBase<TabularStep>
{
    [Parameter] public IList<AnalysisServicesConnectionInfo>? AsConnections { get; set; }

    private AnalysisServicesObjectSelectOffcanvas Offcanvas { get; set; } = null!;

    internal override string FormId => "tabular_step_edit_form";

    protected override TabularStep CreateNewStep(Job job) =>
        new(string.Empty)
        {
            JobId = job.JobId,
            RetryAttempts = 0,
            RetryIntervalMinutes = 0,
            IsEnabled = true,
            ConnectionId = AsConnections?.FirstOrDefault()?.ConnectionId,
            Dependencies = new List<Dependency>(),
            Tags = new List<Tag>(),
            Sources = new List<SourceTargetObject>(),
            Targets = new List<SourceTargetObject>(),
            ExecutionConditionParameters = new List<ExecutionConditionParameter>()
        };

    protected override Task<TabularStep> GetExistingStepAsync(BiflowContext context, Guid stepId) =>
        context.TabularSteps
        .Include(step => step.Tags)
        .Include(step => step.Dependencies)
        .Include(step => step.Sources)
        .Include(step => step.Targets)
        .Include(step => step.ExecutionConditionParameters)
        .FirstAsync(step => step.StepId == stepId);

    private void OnAnalysisServicesObjectSelected((string ModelName, string? TableName, string? PartitionName) obj)
    {
        ArgumentNullException.ThrowIfNull(Step);
        Step.TabularModelName = obj.ModelName;
        Step.TabularTableName = obj.TableName;
        Step.TabularPartitionName = obj.PartitionName;
    }

    protected override (bool Result, string? ErrorMessage) StepValidityCheck(Step step)
    {
        if (step is TabularStep tabular)
        {
            if (!string.IsNullOrEmpty(tabular.TabularPartitionName) && string.IsNullOrEmpty(tabular.TabularTableName))
            {
                return (false, "Table name is required if partition name has been defined");
            }
            else
            {
                return (true, null);
            }
        }
        else
        {
            return (false, "Not TabularStep");
        }
    }

}
