using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

public class DatasetStep : Step
{
    [JsonConstructor]
    public DatasetStep(Guid jobId) : base(StepType.Dataset, jobId) { }

    private DatasetStep(DatasetStep other, Job? targetJob) : base(other, targetJob)
    {
        AppRegistrationId = other.AppRegistrationId;
        AppRegistration = other.AppRegistration;
        DatasetGroupId = other.DatasetGroupId;
        DatasetId = other.DatasetId;
    }

    [Required]
    public Guid? AppRegistrationId { get; set; }

    [Display(Name = "Group id")]
    [MaxLength(36)]
    [MinLength(36)]
    [Required]
    public string? DatasetGroupId { get; set; }

    [Display(Name = "Dataset id")]
    [MaxLength(36)]
    [MinLength(36)]
    [Required]
    public string? DatasetId { get; set; }

    [JsonIgnore]
    public AppRegistration? AppRegistration { get; set; }

    internal override DatasetStep Copy(Job? targetJob = null) => new(this, targetJob);

    internal override StepExecution ToStepExecution(Execution execution) => new DatasetStepExecution(this, execution);
}
