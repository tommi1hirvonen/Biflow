using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class ExeStep : Step, IHasTimeout, IHasStepParameters<ExeStepParameter>
{
    [JsonConstructor]
    public ExeStep() : base(StepType.Exe) { }

    private ExeStep(ExeStep other, Job? targetJob) : base(other, targetJob)
    {
        TimeoutMinutes = other.TimeoutMinutes;
        ExeFileName = other.ExeFileName;
        ExeArguments = other.ExeArguments;
        ExeWorkingDirectory = other.ExeWorkingDirectory;
        ExeSuccessExitCode = other.ExeSuccessExitCode;
        StepParameters = other.StepParameters
            .Select(p => new ExeStepParameter(p, this, targetJob))
            .ToList();
    }

    [Required]
    [Display(Name = "Timeout (min)")]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Required]
    [Display(Name = "File path")]
    [MaxLength(1000)]
    public string? ExeFileName { get; set; }

    [Display(Name = "Arguments")]
    public string? ExeArguments
    {
        get => _exeArguments;
        set => _exeArguments = string.IsNullOrEmpty(value) ? null : value;
    }

    private string? _exeArguments;

    [Display(Name = "Working directory")]
    [MaxLength(1000)]
    public string? ExeWorkingDirectory
    {
        get => _exeWorkingDirectory;
        set => _exeWorkingDirectory = string.IsNullOrEmpty(value) ? null : value;
    }

    private string? _exeWorkingDirectory;

    [Display(Name = "Success exit code")]
    public int? ExeSuccessExitCode { get; set; }

    [ValidateComplexType]
    public IList<ExeStepParameter> StepParameters { get; set; } = null!;

    public override StepExecution ToStepExecution(Execution execution) => new ExeStepExecution(this, execution);

    public override ExeStep Copy(Job? targetJob = null) => new(this, targetJob);
}
