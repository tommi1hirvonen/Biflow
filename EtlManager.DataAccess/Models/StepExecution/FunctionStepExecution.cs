using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace EtlManager.DataAccess.Models;

public class FunctionStepExecution : ParameterizedStepExecution
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
    public int TimeoutMinutes { get; set; }
}
