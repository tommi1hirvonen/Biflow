using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

public class PackageStep : Step, IHasConnection<SqlConnectionInfo>, IHasTimeout, IHasStepParameters<PackageStepParameter>
{
    [JsonConstructor]
    public PackageStep() : base(StepType.Package) { }

    private PackageStep(PackageStep other, Job? targetJob) : base(other, targetJob)
    {
        TimeoutMinutes = other.TimeoutMinutes;
        ConnectionId = other.ConnectionId;
        Connection = other.Connection;
        PackageFolderName = other.PackageFolderName;
        PackageProjectName = other.PackageProjectName;
        PackageName = other.PackageName;
        ExecuteIn32BitMode = other.ExecuteIn32BitMode;
        ExecuteAsLogin = other.ExecuteAsLogin;
        StepParameters = other.StepParameters
            .Select(p => new PackageStepParameter(p, this, targetJob))
            .ToList();
    }

    [Column("TimeoutMinutes")]
    [Required]
    [Display(Name = "Timeout (min)")]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Column("ConnectionId")]
    [Required]
    [NotEmptyGuid]
    public Guid ConnectionId { get; set; }

    [MaxLength(128)]
    [Display(Name = "Folder name")]
    [Required]
    public string? PackageFolderName { get; set; }

    [MaxLength(128)]
    [Display(Name = "Project name")]
    [Required]
    public string? PackageProjectName { get; set; }

    [MaxLength(260)]
    [Display(Name = "Package name")]
    [Required]
    public string? PackageName { get; set; }

    [Required]
    [Display(Name = "32 bit mode")]
    public bool ExecuteIn32BitMode { get; set; }

    [Display(Name = "Execute as login")]
    public string? ExecuteAsLogin
    {
        get => _executeAsLogin;
        set => _executeAsLogin = string.IsNullOrEmpty(value) ? null : value;
    }

    private string? _executeAsLogin;

    [JsonIgnore]
    public SqlConnectionInfo Connection { get; set; } = null!;

    [ValidateComplexType]
    public IList<PackageStepParameter> StepParameters { get; set; } = null!;

    internal override StepExecution ToStepExecution(Execution execution) => new PackageStepExecution(this, execution);

    internal override PackageStep Copy(Job? targetJob = null) => new(this, targetJob);
}
