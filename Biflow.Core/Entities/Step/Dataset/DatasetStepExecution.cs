using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.Core.Entities;

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

    [Display(Name = "Group id")]
    [MaxLength(36)]
    public string DatasetGroupId { get; private set; }

    [Display(Name = "Dataset id")]
    [MaxLength(36)]
    public string DatasetId { get; private set; }

    /// <summary>
    /// Get the <see cref="AppRegistration"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetAppRegistration(AppRegistration?)"/> will need to have been called first for the <see cref="AppRegistration"/> to be available.
    /// </summary>
    /// <returns><see cref="AppRegistration"/> if it was previously set using <see cref="SetAppRegistration(AppRegistration?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public AppRegistration? GetAppRegistration() => _appRegistration;

    /// <summary>
    /// Set the private <see cref="AppRegistration"/> object used for containing a possible app registration reference.
    /// It can be later accessed using <see cref="GetAppRegistration"/>.
    /// </summary>
    /// <param name="appRegistration"><see cref="AppRegistration"/> reference to store.
    /// The AppRegistrationIds are compared and the value is set only if the ids match.</param>
    public void SetAppRegistration(AppRegistration? appRegistration)
    {
        if (appRegistration?.AppRegistrationId == AppRegistrationId)
        {
            _appRegistration = appRegistration;
        }
    }

    // Use a field excluded from the EF model to store the app registration reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    private AppRegistration? _appRegistration;
}
