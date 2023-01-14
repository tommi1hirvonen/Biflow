using System.ComponentModel.DataAnnotations;

namespace Biflow.DataAccess.Models;

public class DatasetStepExecution : StepExecution
{
    public DatasetStepExecution(string stepName, string datasetGroupId, string datasetId) : base(stepName, StepType.Dataset)
    {
        DatasetGroupId = datasetGroupId;
        DatasetId = datasetId;
    }

    [Display(Name = "App registration id")]
    public Guid AppRegistrationId { get; set; }

    public AppRegistration AppRegistration { get; set; } = null!;

    [Display(Name = "Group id")]
    public string DatasetGroupId { get; set; }

    [Display(Name = "Dataset id")]
    public string DatasetId { get; set; }

    public override bool SupportsParameterization => false;
}
