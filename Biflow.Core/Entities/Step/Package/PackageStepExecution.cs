using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class PackageStepExecution : StepExecution,
    IHasTimeout,
    IHasStepExecutionParameters<PackageStepExecutionParameter>,
    IHasStepExecutionAttempts<PackageStepExecutionAttempt>
{
    public PackageStepExecution(string stepName, string packageFolderName, string packageProjectName, string packageName) : base(stepName, StepType.Package)
    {
        PackageFolderName = packageFolderName;
        PackageProjectName = packageProjectName;
        PackageName = packageName;
    }

    public PackageStepExecution(PackageStep step, Execution execution) : base(step, execution)
    {
        ArgumentNullException.ThrowIfNull(step.PackageFolderName);
        ArgumentNullException.ThrowIfNull(step.PackageProjectName);
        ArgumentNullException.ThrowIfNull(step.PackageName);
        ArgumentNullException.ThrowIfNull(step.ConnectionId);

        PackageFolderName = step.PackageFolderName;
        PackageProjectName = step.PackageProjectName;
        PackageName = step.PackageName;
        ExecuteIn32BitMode = step.ExecuteIn32BitMode;
        ExecuteAsLogin = step.ExecuteAsLogin;
        ConnectionId = (Guid)step.ConnectionId;
        TimeoutMinutes = step.TimeoutMinutes;
        StepExecutionParameters = step.StepParameters
            .Select(p => new PackageStepExecutionParameter(p, this))
            .ToArray();
        AddAttempt(new PackageStepExecutionAttempt(this));
    }

    [MaxLength(128)]
    [Display(Name = "Folder name")]
    public string PackageFolderName { get; private set; }

    [MaxLength(128)]
    [Display(Name = "Project name")]
    public string PackageProjectName { get; private set; }

    [MaxLength(260)]
    [Display(Name = "Package name")]
    public string PackageName { get; private set; }

    [Display(Name = "32 bit mode")]
    public bool ExecuteIn32BitMode { get; private set; }

    [Display(Name = "Execute as login")]
    public string? ExecuteAsLogin { get; set; }

    public Guid ConnectionId { get; private set; }

    public double TimeoutMinutes { get; private set; }

    public string? PackagePath => PackageFolderName + "/" + PackageProjectName + "/" + PackageName;

    public IEnumerable<PackageStepExecutionParameter> StepExecutionParameters { get; } = new List<PackageStepExecutionParameter>();

    public override PackageStepExecutionAttempt AddAttempt(StepExecutionStatus withStatus = default)
    {
        var previous = StepExecutionAttempts.MaxBy(x => x.RetryAttemptIndex);
        ArgumentNullException.ThrowIfNull(previous);
        var next = new PackageStepExecutionAttempt((PackageStepExecutionAttempt)previous, previous.RetryAttemptIndex + 1)
        {
            ExecutionStatus = withStatus
        };
        AddAttempt(next);
        return next;
    }

    /// <summary>
    /// Get the <see cref="SqlConnectionInfo"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetConnection(SqlConnectionInfo?)"/> will need to have been called first for the <see cref="SqlConnectionInfo"/> to be available.
    /// </summary>
    /// <returns><see cref="SqlConnectionInfo"/> if it was previously set using <see cref="SetConnection(SqlConnectionInfo?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public SqlConnectionInfo? GetConnection() => _connection;

    /// <summary>
    /// Set the private <see cref="SqlConnectionInfo"/> object used for containing a possible connection reference.
    /// It can be later accessed using <see cref="GetConnection"/>.
    /// </summary>
    /// <param name="connection"><see cref="SqlConnectionInfo"/> reference to store.
    /// The ConnectionIds are compared and the value is set only if the ids match.</param>
    public void SetConnection(SqlConnectionInfo? connection)
    {
        if (connection?.ConnectionId == ConnectionId)
        {
            _connection = connection;
        }
    }

    // Use a field excluded from the EF model to store the connection reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    private SqlConnectionInfo? _connection;
}
