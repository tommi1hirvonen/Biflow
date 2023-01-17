using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

public class FunctionStepExecution : StepExecution, IHasTimeout
{
    public FunctionStepExecution(string stepName, Guid functionAppId, string functionUrl) : base(stepName, StepType.Function)
    {
        FunctionAppId = functionAppId;
        FunctionUrl = functionUrl;
    }

    [Display(Name = "Function app id")]
    public Guid FunctionAppId { get; set; }

    public FunctionApp FunctionApp { get; set; } = null!;

    [Display(Name = "Function url")]
    public string FunctionUrl { get; set; }

    [Display(Name = "Function input")]
    public string? FunctionInput { get; set; }

    [Display(Name = "Is durable")]
    public bool FunctionIsDurable { get; set; }

    [Column("TimeoutMinutes")]
    public double TimeoutMinutes { get; set; }

    public override bool SupportsParameterization => true;
}
