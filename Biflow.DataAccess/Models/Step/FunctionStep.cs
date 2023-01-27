using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class FunctionStep : Step, IHasTimeout, IHasStepParameters<FunctionStepParameter>
{
    public FunctionStep() : base(StepType.Function) { }

    [Column("TimeoutMinutes")]
    [Required]
    [Display(Name = "Timeout (min)")]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Required]
    public Guid? FunctionAppId { get; set; }

    [Display(Name = "Function url")]
    [MaxLength(1000)]
    [Required]
    public string? FunctionUrl { get; set; }

    [Display(Name = "Function input")]
    public string? FunctionInput
    {
        get => _functionInput;
        set => _functionInput = string.IsNullOrEmpty(value) ? null : value;
    }

    private string? _functionInput;

    [Display(Name = "Is durable")]
    public bool FunctionIsDurable { get; set; }

    [Display(Name = "Function key")]
    public string? FunctionKey { get; set; }

    public FunctionApp FunctionApp { get; set; } = null!;

    [ValidateComplexType]
    public IList<FunctionStepParameter> StepParameters { get; set; } = null!;
}
