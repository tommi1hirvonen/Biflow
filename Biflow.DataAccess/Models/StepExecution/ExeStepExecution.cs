using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class ExeStepExecution : StepExecution
{
    public ExeStepExecution(string stepName, string exeFileName) : base(stepName, StepType.Exe)
    {
        ExeFileName = exeFileName;
    }

    [Display(Name = "File path")]
    public string ExeFileName { get; set; }

    [Display(Name = "Arguments")]
    public string? ExeArguments { get; set; }

    [Display(Name = "Working directory")]
    public string? ExeWorkingDirectory { get; set; }

    [Display(Name = "Success exit code")]
    public int? ExeSuccessExitCode { get; set; }

    [Column("TimeoutMinutes")]
    public double TimeoutMinutes { get; set; }
}
