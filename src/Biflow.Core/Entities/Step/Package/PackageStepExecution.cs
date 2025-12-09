using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

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
        PackageFolderName = step.PackageFolderName;
        PackageProjectName = step.PackageProjectName;
        PackageName = step.PackageName;
        ExecuteIn32BitMode = step.ExecuteIn32BitMode;
        ExecuteAsLogin = step.ExecuteAsLogin;
        ConnectionId = step.ConnectionId;
        TimeoutMinutes = step.TimeoutMinutes;
        StepExecutionParameters = step.StepParameters
            .Select(p => new PackageStepExecutionParameter(p, this))
            .ToArray();
        AddAttempt(new PackageStepExecutionAttempt(this));
    }

    [MaxLength(128)]
    public string PackageFolderName { get; [UsedImplicitly] private set; }

    [MaxLength(128)]
    public string PackageProjectName { get; [UsedImplicitly] private set; }

    [MaxLength(260)]
    public string PackageName { get; [UsedImplicitly] private set; }

    public bool ExecuteIn32BitMode { get; private set; }

    public string? ExecuteAsLogin { get; private set; }

    public Guid ConnectionId { get; [UsedImplicitly] private set; }

    public double TimeoutMinutes { get; [UsedImplicitly] private set; }

    public string PackagePath => PackageFolderName + "/" + PackageProjectName + "/" + PackageName;

    public IEnumerable<PackageStepExecutionParameter> StepExecutionParameters { get; } = new List<PackageStepExecutionParameter>();
    
    public override DisplayStepType DisplayStepType => DisplayStepType.Package;

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
    /// Get the <see cref="MsSqlConnection"/> entity associated with this <see cref="StepExecution"/>.
    /// The method <see cref="SetConnection(MsSqlConnection?)"/> will need to have been called first for the <see cref="MsSqlConnection"/> to be available.
    /// </summary>
    /// <returns><see cref="MsSqlConnection"/> if it was previously set using <see cref="SetConnection(MsSqlConnection?)"/> with a non-null object; <see langword="null"/> otherwise.</returns>
    public MsSqlConnection? GetConnection() => _connection;

    /// <summary>
    /// Set the private <see cref="MsSqlConnection"/> object used for containing a possible connection reference.
    /// It can be later accessed using <see cref="GetConnection"/>.
    /// </summary>
    /// <param name="connection"><see cref="MsSqlConnection"/> reference to store.
    /// The ConnectionIds are compared and the value is set only if the ids match.</param>
    public void SetConnection(MsSqlConnection? connection)
    {
        if (connection?.ConnectionId == ConnectionId)
        {
            _connection = connection;
        }
    }

    // Use a field excluded from the EF model to store the connection reference.
    // This is to avoid generating a foreign key constraint on the ExecutionStep table caused by a navigation property.
    // Make it private with public method access so that it is not used in EF Include method calls by accident.
    private MsSqlConnection? _connection;
}
