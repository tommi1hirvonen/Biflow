using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class DatasetStepExecution : StepExecution
{
    public DatasetStepExecution(string stepName, string datasetGroupId, string datasetId) : base(stepName, StepType.Dataset)
    {
        DatasetGroupId = datasetGroupId;
        DatasetId = datasetId;
    }

    public DatasetStepExecution(DatasetStep step, Execution execution) : base(step, execution)
    {
        AppRegistrationId = step.AppRegistrationId;
        DatasetGroupId = step.DatasetGroupId;
        DatasetId = step.DatasetId;

        StepExecutionAttempts = new[] { new DatasetStepExecutionAttempt(this) };
    }

    [Display(Name = "App registration id")]
    public Guid AppRegistrationId { get; private set; }

    public AppRegistration AppRegistration { get; set; } = null!;

    [Display(Name = "Group id")]
    [MaxLength(36)]
    public string DatasetGroupId { get; private set; }

    [Display(Name = "Dataset id")]
    [MaxLength(36)]
    public string DatasetId { get; private set; }

}
