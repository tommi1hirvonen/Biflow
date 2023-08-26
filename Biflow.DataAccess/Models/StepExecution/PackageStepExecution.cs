using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class PackageStepExecution : StepExecution, IHasTimeout, IHasStepExecutionParameters<PackageStepExecutionParameter>
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
        StepExecutionAttempts = new[] { new PackageStepExecutionAttempt(this) };
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

    [Column("ConnectionId")]
    public Guid ConnectionId { get; private set; }

    public SqlConnectionInfo Connection { get; set; } = null!;

    [Column("TimeoutMinutes")]
    public double TimeoutMinutes { get; private set; }

    [NotMapped]
    public string? PackagePath => PackageFolderName + "/" + PackageProjectName + "/" + PackageName;

    public IList<PackageStepExecutionParameter> StepExecutionParameters { get; set; } = null!;
}
