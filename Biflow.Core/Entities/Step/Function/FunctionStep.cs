using Biflow.Core.Attributes;
using Biflow.Core.Attributes.Validation;
using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

public class FunctionStep : Step, IHasTimeout, IHasStepParameters<FunctionStepParameter>
{
    [JsonConstructor]
    public FunctionStep() : base(StepType.Function) { }

    private FunctionStep(FunctionStep other, Job? targetJob) : base(other, targetJob)
    {
        TimeoutMinutes = other.TimeoutMinutes;
        FunctionAppId = other.FunctionAppId;
        FunctionApp = other.FunctionApp;
        FunctionUrl = other.FunctionUrl;
        FunctionInput = other.FunctionInput;
        FunctionInputFormat = other.FunctionInputFormat;
        FunctionIsDurable = other.FunctionIsDurable;
        FunctionKey = other.FunctionKey;
        StepParameters = other.StepParameters
            .Select(p => new FunctionStepParameter(p, this, targetJob))
            .ToList();
    }
        
    [Required]
    [Range(0, 2880)] // 48 hours
    public double TimeoutMinutes { get; set; }

    [Required]
    [NotEmptyGuid]
    public Guid FunctionAppId { get; set; }

    [MaxLength(1000)]
    [Required]
    public string FunctionUrl { get; set; } = "";

    public string? FunctionInput
    {
        get;
        set => field = string.IsNullOrEmpty(value) ? null : value;
    }
    
    public FunctionInputFormat FunctionInputFormat { get; set; } = FunctionInputFormat.PlainText;

    public bool FunctionIsDurable { get; set; }

    [MaxLength(1000)]
    [JsonSensitive]
    public string? FunctionKey
    {
        get;
        set => field = string.IsNullOrEmpty(value) ? null : value;
    }

    [JsonIgnore]
    public FunctionApp FunctionApp { get; set; } = null!;

    [ValidateComplexType]
    [JsonInclude]
    public IList<FunctionStepParameter> StepParameters { get; private set; } = new List<FunctionStepParameter>();

    public override StepExecution ToStepExecution(Execution execution) => new FunctionStepExecution(this, execution);

    public override FunctionStep Copy(Job? targetJob = null) => new(this, targetJob);
}
