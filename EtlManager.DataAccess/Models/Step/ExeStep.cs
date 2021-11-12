using System.ComponentModel.DataAnnotations;

namespace EtlManager.DataAccess.Models;

public class ExeStep : Step
{
    public ExeStep() : base(StepType.Exe) { }

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
}
