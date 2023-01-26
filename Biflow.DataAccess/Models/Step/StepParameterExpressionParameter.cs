using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("StepParameterExpressionParameter")]
[PrimaryKey("StepParameterId", "ParameterName")]
public class StepParameterExpressionParameter : IAsyncEvaluable
{
    public Guid StepParameterId { get; set; }

    public StepParameterBase StepParameter { get; set; } = null!;

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string ParameterName { get; set; } = "";

    public Guid InheritFromJobParameterId { get; set; }

    public JobParameter InheritFromJobParameter { get; set; } = null!;

    public async Task<object?> EvaluateAsync() => await InheritFromJobParameter.EvaluateAsync();
}
