using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class PackageStep : Step
{
    public PackageStep() : base(StepType.Package) { }

    [Column("ConnectionId")]
    [Required]
    public Guid? ConnectionId { get; set; }

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

    public override bool SupportsParameterization => true;

    public SqlConnectionInfo Connection { get; set; } = null!;
}
