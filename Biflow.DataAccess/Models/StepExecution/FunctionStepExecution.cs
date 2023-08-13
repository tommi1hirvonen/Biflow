using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class FunctionStepExecution : StepExecution, IHasTimeout, IHasStepExecutionParameters<FunctionStepExecutionParameter>
{
    public FunctionStepExecution(string stepName, Guid functionAppId, string functionUrl) : base(stepName, StepType.Function)
    {
        FunctionAppId = functionAppId;
        FunctionUrl = functionUrl;
    }

    [Display(Name = "Function app id")]
    public Guid FunctionAppId { get; private set; }

    public FunctionApp FunctionApp { get; set; } = null!;

    [Display(Name = "Function url")]
    public string FunctionUrl { get; private set; }

    [Display(Name = "Function input")]
    public string? FunctionInput { get; private set; }

    [Display(Name = "Is durable")]
    public bool FunctionIsDurable { get; private set; }

    [Column("TimeoutMinutes")]
    public double TimeoutMinutes { get; private set; }

    public IList<FunctionStepExecutionParameter> StepExecutionParameters { get; set; } = null!;
}
