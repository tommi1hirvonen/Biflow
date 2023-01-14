using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class PackageStepExecution : StepExecution
{
    public PackageStepExecution(string stepName, string packageFolderName, string packageProjectName, string packageName) : base(stepName, StepType.Package)
    {
        PackageFolderName = packageFolderName;
        PackageProjectName = packageProjectName;
        PackageName = packageName;
    }

    [MaxLength(128)]
    [Display(Name = "Folder name")]
    public string PackageFolderName { get; set; }

    [MaxLength(128)]
    [Display(Name = "Project name")]
    public string PackageProjectName { get; set; }

    [MaxLength(260)]
    [Display(Name = "Package name")]
    public string PackageName { get; set; }

    [Display(Name = "32 bit mode")]
    public bool ExecuteIn32BitMode { get; set; }

    [Display(Name = "Execute as login")]
    public string? ExecuteAsLogin { get; set; }

    [Column("ConnectionId")]
    public Guid ConnectionId { get; set; }

    public SqlConnectionInfo Connection { get; set; } = null!;

    [Column("TimeoutMinutes")]
    public double TimeoutMinutes { get; set; }

    [NotMapped]
    public string? PackagePath => PackageFolderName + "/" + PackageProjectName + "/" + PackageName;

    public override bool SupportsParameterization => true;
}
