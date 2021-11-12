using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManager.DataAccess.Models;

public abstract class ParameterizedStep : Step
{
    public ParameterizedStep(StepType stepType) : base(stepType)
    {
    }

    public IList<StepParameterBase> StepParameters { get; set; } = null!;
}
