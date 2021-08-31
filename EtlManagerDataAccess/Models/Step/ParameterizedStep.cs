using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public abstract record ParameterizedStep : Step
    {
        public ParameterizedStep(StepType stepType) : base(stepType)
        {
        }

        public IList<StepParameter> StepParameters { get; set; } = null!;
    }
}
