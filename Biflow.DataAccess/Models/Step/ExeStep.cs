using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class ExeStep : Step, IHasTimeout, IHasStepParameters<ExeStepParameter>
{
    public ExeStep() : base(StepType.Exe) { }

    [Column("TimeoutMinutes")]
    [Required]
    [Display(Name = "Timeout (min)")]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Required]
    [Display(Name = "File path")]
    public string? ExeFileName { get; set; }

    [Display(Name = "Arguments")]
    public string? ExeArguments
    {
        get => _exeArguments;
        set => _exeArguments = string.IsNullOrEmpty(value) ? null : value;
    }

    private string? _exeArguments;

    [Display(Name = "Working directory")]
    public string? ExeWorkingDirectory
    {
        get => _exeWorkingDirectory;
        set => _exeWorkingDirectory = string.IsNullOrEmpty(value) ? null : value;
    }

    private string? _exeWorkingDirectory;

    [Display(Name = "Success exit code")]
    public int? ExeSuccessExitCode { get; set; }

    public IList<ExeStepParameter> StepParameters { get; set; } = null!;
}
